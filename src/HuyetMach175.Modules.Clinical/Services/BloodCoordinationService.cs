using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodCoordination;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Mappings;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Services;

public class BloodCoordinationService : IBloodCoordinationService
{
    private readonly ApplicationDbContext _context;

    public BloodCoordinationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupplyDemandResponse> GetSupplyDemandAsync(int componentTypeId)
    {
        if (componentTypeId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã loại chế phẩm máu (componentTypeId) không hợp lệ.");
        }

        var componentType = await _context.BloodComponentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ComponentTypeId == componentTypeId);

        if (componentType == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy loại chế phẩm máu với ID #{componentTypeId}.");
        }

        var now = DateTime.UtcNow;

        var stockByGroup = await _context.BloodBags
            .AsNoTracking()
            .Where(b => b.Status == BagStatus.AVAILABLE
                     && b.ComponentTypeId == componentTypeId
                     && b.ExpiredAt > now)
            .GroupBy(b => new { b.BloodType, b.RhFactor })
            .Select(g => new { g.Key.BloodType, g.Key.RhFactor, Count = g.Count() })
            .ToListAsync();

        var demandByGroup = await _context.BloodRequestItems
            .AsNoTracking()
            .Where(i => i.ComponentTypeId == componentTypeId
                     && i.Request != null
                     && i.Request.Status == RequestStatus.PENDING
                     && i.RequestedUnits > i.AllocatedUnits)
            .GroupBy(i => new { i.Request!.PatientBloodType, i.Request.PatientRh })
            .Select(g => new
            {
                BloodType = g.Key.PatientBloodType,
                RhFactor = g.Key.PatientRh,
                Units = g.Sum(x => x.RequestedUnits - x.AllocatedUnits)
            })
            .ToListAsync();

        var stockDict = stockByGroup.ToDictionary(x => (x.BloodType, x.RhFactor), x => x.Count);
        var demandDict = demandByGroup.ToDictionary(x => (x.BloodType, x.RhFactor), x => x.Units);

        var details = new List<SupplyDemandDetailDto>();
        var criticalShortages = new List<string>();
        var totalAvailableStock = 0;
        var totalPendingDemand = 0;

        foreach (var group in BloodCompatibilityRules.StandardBloodGroups)
        {
            var stock = stockDict.GetValueOrDefault(group, 0);
            var demand = demandDict.GetValueOrDefault(group, 0);
            var shortage = Math.Max(0, demand - stock);
            var isShortage = shortage > 0;
            var label = BloodCompatibilityRules.FormatBloodGroup(group.BloodType, group.RhFactor);

            totalAvailableStock += stock;
            totalPendingDemand += demand;

            if (isShortage)
            {
                criticalShortages.Add(label);
            }

            details.Add(new SupplyDemandDetailDto
            {
                BloodType = label,
                StockUnits = stock,
                DemandUnits = demand,
                IsShortage = isShortage,
                ShortageUnits = shortage
            });
        }

        return new SupplyDemandResponse
        {
            ComponentTypeId = componentType.ComponentTypeId,
            ComponentCode = componentType.TypeCode,
            ComponentName = componentType.TypeName,
            TotalAvailableStock = totalAvailableStock,
            TotalPendingDemand = totalPendingDemand,
            CriticalShortages = criticalShortages,
            Details = details
        };
    }

    public async Task<PagedResult<CoordinationQueueItemDto>> GetCoordinationQueueAsync(BloodCoordinationQueueFilterDto filter)
    {
        BloodRequestProcessing.ValidatePagination(filter.PageNumber, filter.PageSize);

        var query = _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.Department)
            .Include(r => r.Items)
                .ThenInclude(i => i.ComponentType)
            .Where(r => r.Status == RequestStatus.PENDING);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(r =>
                EF.Functions.ILike(r.RequestCode, $"%{kw}%") ||
                EF.Functions.ILike(r.PatientCode, $"%{kw}%") ||
                EF.Functions.ILike(r.PatientName, $"%{kw}%") ||
                (r.Doctor != null && r.Doctor.Department != null && EF.Functions.ILike(r.Doctor.Department.DepartmentName, $"%{kw}%")));
        }

        if (!string.IsNullOrWhiteSpace(filter.UrgencyLevel))
        {
            if (!Enum.TryParse<UrgencyLevel>(filter.UrgencyLevel.Trim(), true, out var parsedUrgency))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Mức độ khẩn cấp '{filter.UrgencyLevel}' không hợp lệ.");
            }
            query = query.Where(r => r.UrgencyLevel == parsedUrgency);
        }

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderBy(r => r.UrgencyLevel == UrgencyLevel.EMERGENCY ? 0 : r.UrgencyLevel == UrgencyLevel.URGENT ? 1 : 2)
            .ThenBy(r => r.CreatedAt)
            .ThenBy(r => r.RequestId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<CoordinationQueueItemDto>
        {
            Items = records.Select(r => r.ToQueueItemDto()).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<AvailableBloodBagDto>> GetAvailableBagsForItemAsync(int requestId, int requestItemId, bool includeCompatible)
    {
        if (requestId <= 0 || requestItemId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu hoặc mã dòng chế phẩm không hợp lệ.");
        }

        var bloodRequest = await _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Items)
                .ThenInclude(i => i.ComponentType)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (bloodRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{requestId}.");
        }

        if (bloodRequest.Status != RequestStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu {bloodRequest.RequestCode} đang ở trạng thái '{bloodRequest.Status}', không thể chọn giữ chỗ túi máu.");
        }

        var requestItem = bloodRequest.Items.FirstOrDefault(i => i.RequestItemId == requestItemId);
        if (requestItem == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy dòng y lệnh #{requestItemId} trong phiếu yêu cầu #{requestId}.");
        }

        var componentCode = requestItem.ComponentType?.TypeCode ?? ComponentCode.WB;
        var now = DateTime.UtcNow;

        var candidateBags = await _context.BloodBags
            .AsNoTracking()
            .Include(b => b.Location)
            .Where(b => b.Status == BagStatus.AVAILABLE
                     && b.ComponentTypeId == requestItem.ComponentTypeId
                     && b.ExpiredAt > now
                     && !_context.BloodAllocations.Any(a => a.BagId == b.BagId &&
                         (a.Status == AllocationStatus.RESERVED || a.Status == AllocationStatus.ISSUED)))
            .OrderBy(b => b.ExpiredAt)
            .ThenBy(b => b.BagId)
            .ToListAsync();

        return candidateBags
            .Where(b => includeCompatible
                ? BloodCompatibilityRules.IsCompatible(componentCode, bloodRequest.PatientBloodType, bloodRequest.PatientRh, b.BloodType, b.RhFactor)
                : BloodCompatibilityRules.IsExactMatch(bloodRequest.PatientBloodType, bloodRequest.PatientRh, b.BloodType, b.RhFactor))
            .OrderByDescending(b => BloodCompatibilityRules.IsExactMatch(bloodRequest.PatientBloodType, bloodRequest.PatientRh, b.BloodType, b.RhFactor))
            .ThenBy(b => b.ExpiredAt)
            .ThenBy(b => b.BagId)
            .Select(b => b.ToAvailableBagDto(bloodRequest.PatientBloodType, bloodRequest.PatientRh, componentCode))
            .ToList();
    }

    public async Task<AllocateBloodBagsResponse> AllocateBagsAsync(int requestId, int requestItemId, AllocateBloodBagsRequest request, int userId, string? ipAddress)
    {
        await BloodRequestProcessing.EnsureActiveUserAsync(_context, userId, "Thông tin người thực hiện giữ chỗ không hợp lệ.");

        var distinctBagIds = ValidateAllocationInput(requestId, requestItemId, request.BagIds);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var bloodRequest = await _context.BloodRequests
            .Include(r => r.Items)
                .ThenInclude(i => i.ComponentType)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (bloodRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{requestId}.");
        }

        if (bloodRequest.Status != RequestStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu {bloodRequest.RequestCode} đang ở trạng thái '{bloodRequest.Status}', chỉ có thể giữ chỗ khi đang PENDING.");
        }

        var requestItem = bloodRequest.Items.FirstOrDefault(i => i.RequestItemId == requestItemId);
        if (requestItem == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy dòng y lệnh #{requestItemId} trong phiếu yêu cầu #{requestId}.");
        }

        var remainingUnits = requestItem.RequestedUnits - requestItem.AllocatedUnits;
        if (remainingUnits <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Dòng y lệnh này đã được giữ chỗ đủ số lượng yêu cầu.");
        }

        if (distinctBagIds.Count > remainingUnits)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Số lượng túi chọn ({distinctBagIds.Count}) vượt quá số lượng còn thiếu ({remainingUnits}) của y lệnh.");
        }

        var now = DateTime.UtcNow;
        var bags = await LoadAndValidateSelectedBagsAsync(distinctBagIds, bloodRequest, requestItem, now);

        var oldRequestStatus = bloodRequest.Status;
        var oldItemAllocated = requestItem.AllocatedUnits;

        foreach (var bag in bags)
        {
            bag.Status = BagStatus.RESERVED;
            _context.BloodAllocations.Add(new BloodAllocation
            {
                RequestId = requestId,
                BagId = bag.BagId,
                ReservedBy = userId,
                ReservedAt = now,
                Status = AllocationStatus.RESERVED
            });
        }

        requestItem.AllocatedUnits += bags.Count;

        if (bloodRequest.Items.All(i => i.AllocatedUnits >= i.RequestedUnits))
        {
            bloodRequest.Status = RequestStatus.READY;
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_requests",
            RecordId = bloodRequest.RequestId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                requestStatus = oldRequestStatus.ToString(),
                requestItemId = requestItem.RequestItemId,
                allocatedUnits = oldItemAllocated
            }),
            NewState = JsonSerializer.Serialize(new
            {
                requestStatus = bloodRequest.Status.ToString(),
                requestItemId = requestItem.RequestItemId,
                allocatedUnits = requestItem.AllocatedUnits,
                bagIds = bags.Select(b => b.BagId).ToList(),
                barcodes = bags.Select(b => b.Barcode).ToList()
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Giữ chỗ {bags.Count} túi máu cho phiếu {bloodRequest.RequestCode}. Trạng thái phiếu: {bloodRequest.Status}.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        foreach (var bag in bags)
        {
            _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
            {
                BagId = bag.BagId,
                FromStatus = BagStatus.AVAILABLE,
                ToStatus = BagStatus.RESERVED,
                PerformedById = userId,
                AuditLogId = auditLog.LogId,
                Notes = $"Giữ chỗ cho phiếu yêu cầu {bloodRequest.RequestCode}",
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new AllocateBloodBagsResponse
        {
            RequestId = bloodRequest.RequestId,
            RequestCode = bloodRequest.RequestCode,
            RequestItemId = requestItem.RequestItemId,
            AllocatedCount = bags.Count,
            ItemAllocatedUnits = requestItem.AllocatedUnits,
            ItemRequestedUnits = requestItem.RequestedUnits,
            RequestStatus = bloodRequest.Status,
            CurrentStep = BloodRequestMappings.ResolveCurrentStep(bloodRequest.Status),
            AllocatedBags = bags
                .OrderBy(b => b.ExpiredAt)
                .Select(b => b.ToAllocatedBagSummaryDto(bloodRequest.PatientBloodType, bloodRequest.PatientRh))
                .ToList()
        };
    }

    private static List<int> ValidateAllocationInput(int requestId, int requestItemId, List<int>? bagIds)
    {
        if (requestId <= 0 || requestItemId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu hoặc mã dòng chế phẩm không hợp lệ.");
        }

        if (bagIds == null || bagIds.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng chọn ít nhất 1 túi máu để giữ chỗ.");
        }

        if (bagIds.Any(id => id <= 0))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Danh sách ID túi máu chứa giá trị không hợp lệ.");
        }

        var distinctBagIds = bagIds.Distinct().ToList();
        if (distinctBagIds.Count != bagIds.Count)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Danh sách ID túi máu không được chứa mã trùng lặp.");
        }

        return distinctBagIds;
    }

    private async Task<List<BloodBag>> LoadAndValidateSelectedBagsAsync(
        List<int> distinctBagIds,
        BloodRequest bloodRequest,
        BloodRequestItem requestItem,
        DateTime now)
    {
        var bags = await _context.BloodBags
            .Where(b => distinctBagIds.Contains(b.BagId))
            .ToListAsync();

        var foundIds = bags.Select(b => b.BagId).ToHashSet();
        var missingIds = distinctBagIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy các túi máu với ID: {string.Join(", ", missingIds)}.");
        }

        var componentCode = requestItem.ComponentType?.TypeCode ?? ComponentCode.WB;

        foreach (var bag in bags)
        {
            if (bag.ComponentTypeId != requestItem.ComponentTypeId)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} không khớp loại chế phẩm của y lệnh.");
            }

            if (!BloodCompatibilityRules.IsCompatible(componentCode, bloodRequest.PatientBloodType, bloodRequest.PatientRh, bag.BloodType, bag.RhFactor))
            {
                var bagGroup = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor);
                var patientGroup = BloodCompatibilityRules.FormatBloodGroup(bloodRequest.PatientBloodType, bloodRequest.PatientRh);
                throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} (nhóm {bagGroup}) không tương thích miễn dịch với người bệnh (nhóm {patientGroup}).");
            }
        }

        var activeAllocations = await _context.BloodAllocations
            .AsNoTracking()
            .Include(a => a.Request)
            .Include(a => a.Bag)
            .Where(a => distinctBagIds.Contains(a.BagId) &&
                       (a.Status == AllocationStatus.RESERVED || a.Status == AllocationStatus.ISSUED))
            .ToListAsync();

        if (activeAllocations.Count > 0)
        {
            var sameRequestBags = activeAllocations
                .Where(a => a.RequestId == bloodRequest.RequestId)
                .Select(a => a.Bag?.Barcode ?? a.BagId.ToString())
                .Distinct()
                .ToList();

            if (sameRequestBags.Count > 0)
            {
                throw new HttpException(StatusCodes.Status409Conflict, $"Các túi máu sau đã được giữ chỗ hoặc cấp phát cho chính phiếu {bloodRequest.RequestCode}: {string.Join(", ", sameRequestBags)}.");
            }

            var otherDetails = activeAllocations
                .Select(a => $"{a.Bag?.Barcode ?? a.BagId.ToString()} ({a.Request?.RequestCode ?? $"#{a.RequestId}"})")
                .Distinct()
                .ToList();

            throw new HttpException(StatusCodes.Status409Conflict, $"Các túi máu sau đã được giữ chỗ hoặc cấp phát cho phiếu khác: {string.Join(", ", otherDetails)}.");
        }

        foreach (var bag in bags)
        {
            if (bag.Status != BagStatus.AVAILABLE)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đang ở trạng thái '{bag.Status}', chỉ túi máu 'AVAILABLE' mới có thể giữ chỗ.");
            }

            if (bag.ExpiredAt <= now)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đã hết hạn sử dụng.");
            }
        }

        return bags;
    }
}
