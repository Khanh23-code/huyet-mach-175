using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Mappings;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Services;

public class BloodRequestsService : IBloodRequestsService
{
    private readonly ApplicationDbContext _context;

    public BloodRequestsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateBloodRequestResponse> CreateBloodRequestAsync(CreateBloodRequestDto request, int doctorId, string? ipAddress)
    {
        await EnsureActiveUserAsync(doctorId, "Thông tin bác sĩ đăng nhập không hợp lệ.");

        var patientCode = request.PatientCode?.Trim() ?? string.Empty;
        var patientName = request.PatientName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(patientCode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã người bệnh (patientCode) không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(patientName))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Tên người bệnh (patientName) không được để trống.");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiếu yêu cầu máu phải có ít nhất 1 loại chế phẩm.");
        }

        var duplicateComponentIds = request.Items
            .GroupBy(i => i.ComponentTypeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateComponentIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Không được chọn trùng loại chế phẩm máu trong cùng một phiếu yêu cầu.");
        }

        if (request.Items.Any(i => i.RequestedUnits <= 0))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số lượng đơn vị yêu cầu cho mỗi chế phẩm phải lớn hơn 0.");
        }

        var componentTypeIds = request.Items.Select(i => i.ComponentTypeId).Distinct().ToList();
        var existingComponentIds = await _context.BloodComponentTypes
            .AsNoTracking()
            .Where(c => componentTypeIds.Contains(c.ComponentTypeId))
            .Select(c => c.ComponentTypeId)
            .ToHashSetAsync();

        var missingComponentIds = componentTypeIds.Where(id => !existingComponentIds.Contains(id)).ToList();
        if (missingComponentIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy loại chế phẩm máu với ID: {string.Join(", ", missingComponentIds)}.");
        }

        var now = DateTime.UtcNow;
        var localDate = now.AddHours(7);
        var prefix = $"YC-{localDate:yyMMdd}-";

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var countToday = await _context.BloodRequests
            .CountAsync(r => r.RequestCode.StartsWith(prefix));
        var seq = countToday + 1;
        var requestCode = $"{prefix}{seq:D3}";

        while (await _context.BloodRequests.AnyAsync(r => r.RequestCode == requestCode))
        {
            seq++;
            requestCode = $"{prefix}{seq:D3}";
        }

        var status = request.IsDraft ? RequestStatus.DRAFT : RequestStatus.PENDING;

        var entity = new BloodRequest
        {
            RequestCode = requestCode,
            DoctorId = doctorId,
            PatientCode = patientCode,
            PatientName = patientName,
            PatientBloodType = request.PatientBloodType,
            PatientRh = request.PatientRh,
            UrgencyLevel = request.UrgencyLevel,
            Diagnosis = request.Diagnosis?.Trim(),
            Status = status,
            CreatedAt = now
        };

        _context.BloodRequests.Add(entity);
        await _context.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            _context.BloodRequestItems.Add(new BloodRequestItem
            {
                RequestId = entity.RequestId,
                ComponentTypeId = item.ComponentTypeId,
                RequestedUnits = item.RequestedUnits,
                AllocatedUnits = 0
            });
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_requests",
            RecordId = entity.RequestId,
            ActionType = ActionType.INSERT,
            OldState = null,
            NewState = JsonSerializer.Serialize(new
            {
                requestId = entity.RequestId,
                requestCode = entity.RequestCode,
                doctorId = entity.DoctorId,
                patientCode = entity.PatientCode,
                patientName = entity.PatientName,
                patientBloodType = entity.PatientBloodType.ToString(),
                patientRh = entity.PatientRh.ToString(),
                urgencyLevel = entity.UrgencyLevel.ToString(),
                status = entity.Status.ToString(),
                totalRequestedUnits = request.Items.Sum(i => i.RequestedUnits)
            }),
            PerformedBy = doctorId,
            IpAddress = ipAddress,
            ChangeReason = request.IsDraft
                ? $"Lưu nháp phiếu yêu cầu máu {entity.RequestCode} cho người bệnh {entity.PatientName} ({entity.PatientCode})."
                : $"Tạo và gửi phiếu yêu cầu máu {entity.RequestCode} cho người bệnh {entity.PatientName} ({entity.PatientCode}).",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new CreateBloodRequestResponse
        {
            RequestId = entity.RequestId,
            RequestCode = entity.RequestCode,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<PagedResult<ClinicalBloodRequestDto>> GetClinicalBloodRequestsAsync(ClinicalBloodRequestFilterDto filter, int doctorId)
    {
        if (doctorId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Thông tin bác sĩ đăng nhập không hợp lệ.");
        }

        ValidatePagination(filter.PageNumber, filter.PageSize);

        var query = _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.DoctorId == doctorId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(r =>
                EF.Functions.ILike(r.RequestCode, $"%{kw}%") ||
                EF.Functions.ILike(r.PatientCode, $"%{kw}%") ||
                EF.Functions.ILike(r.PatientName, $"%{kw}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.UrgencyLevel))
        {
            if (!Enum.TryParse<UrgencyLevel>(filter.UrgencyLevel.Trim(), true, out var parsedUrgency))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Mức độ khẩn cấp '{filter.UrgencyLevel}' không hợp lệ.");
            }
            query = query.Where(r => r.UrgencyLevel == parsedUrgency);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (!Enum.TryParse<RequestStatus>(filter.Status.Trim(), true, out var parsedStatus))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Trạng thái phiếu '{filter.Status}' không hợp lệ.");
            }
            query = query.Where(r => r.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RequestId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ClinicalBloodRequestDto>
        {
            Items = records.Select(r => r.ToClinicalDto()).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<BloodRequestDetailDto> GetBloodRequestByIdAsync(int id, int currentUserId)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu máu không hợp lệ.");
        }

        var bloodRequest = await _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.Department)
            .Include(r => r.Items)
                .ThenInclude(i => i.ComponentType)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.Bag)
                    .ThenInclude(b => b!.ComponentType)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.ReservedByUser)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.IssuedByUser)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.ReceivedByUser)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (bloodRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{id}.");
        }

        if (bloodRequest.DoctorId != currentUserId)
        {
            var canViewAll = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == currentUserId && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .Where(ur => ur.Role != null && ur.Role.IsActive)
                .AnyAsync(ur =>
                    ur.Role!.RoleCode == "SYS" ||
                    ur.Role.RoleCode == "MGT" ||
                    ur.Role.RoleCode == "BBNK" ||
                    ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.PermissionCode == "BLOOD_REQUEST_APPROVE"));

            if (!canViewAll)
            {
                throw new HttpException(StatusCodes.Status403Forbidden, "Bạn chỉ có quyền xem chi tiết phiếu yêu cầu máu do chính mình tạo.");
            }
        }

        return bloodRequest.ToDetailDto();
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
        ValidatePagination(filter.PageNumber, filter.PageSize);

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

        var filteredBags = candidateBags
            .Where(b => includeCompatible
                ? BloodCompatibilityRules.IsCompatible(componentCode, bloodRequest.PatientBloodType, bloodRequest.PatientRh, b.BloodType, b.RhFactor)
                : BloodCompatibilityRules.IsExactMatch(bloodRequest.PatientBloodType, bloodRequest.PatientRh, b.BloodType, b.RhFactor))
            .OrderByDescending(b => BloodCompatibilityRules.IsExactMatch(bloodRequest.PatientBloodType, bloodRequest.PatientRh, b.BloodType, b.RhFactor))
            .ThenBy(b => b.ExpiredAt)
            .ThenBy(b => b.BagId)
            .Select(b => b.ToAvailableBagDto(bloodRequest.PatientBloodType, bloodRequest.PatientRh, componentCode))
            .ToList();

        return filteredBags;
    }

    public async Task<AllocateBloodBagsResponse> AllocateBagsAsync(int requestId, int requestItemId, AllocateBloodBagsRequest request, int userId, string? ipAddress)
    {
        await EnsureActiveUserAsync(userId, "Thông tin người thực hiện giữ chỗ không hợp lệ.");

        if (requestId <= 0 || requestItemId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu hoặc mã dòng chế phẩm không hợp lệ.");
        }

        if (request.BagIds == null || request.BagIds.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng chọn ít nhất 1 túi máu để giữ chỗ.");
        }

        if (request.BagIds.Any(id => id <= 0))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Danh sách ID túi máu chứa giá trị không hợp lệ.");
        }

        var distinctBagIds = request.BagIds.Distinct().ToList();
        if (distinctBagIds.Count != request.BagIds.Count)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Danh sách ID túi máu không được chứa mã trùng lặp.");
        }

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

        var bags = await _context.BloodBags
            .Where(b => distinctBagIds.Contains(b.BagId))
            .ToListAsync();

        var foundIds = bags.Select(b => b.BagId).ToHashSet();
        var missingIds = distinctBagIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy các túi máu với ID: {string.Join(", ", missingIds)}.");
        }

        var activeAllocatedBagIds = await _context.BloodAllocations
            .AsNoTracking()
            .Where(a => distinctBagIds.Contains(a.BagId) &&
                       (a.Status == AllocationStatus.RESERVED || a.Status == AllocationStatus.ISSUED))
            .Select(a => a.BagId)
            .ToListAsync();

        if (activeAllocatedBagIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Các túi máu sau đã được giữ chỗ hoặc cấp phát cho phiếu khác: {string.Join(", ", activeAllocatedBagIds)}.");
        }

        var now = DateTime.UtcNow;
        var componentCode = requestItem.ComponentType?.TypeCode ?? ComponentCode.WB;

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

    public async Task<RejectBloodRequestResponse> RejectBloodRequestAsync(int id, RejectBloodRequestDto request, int userId, string? ipAddress)
    {
        await EnsureActiveUserAsync(userId, "Thông tin người từ chối không hợp lệ.");

        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu máu không hợp lệ.");
        }

        var reason = request.RejectionReason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do từ chối (rejectionReason) không được để trống.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var bloodRequest = await _context.BloodRequests
            .Include(r => r.Items)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.Bag)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (bloodRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{id}.");
        }

        if (bloodRequest.Status != RequestStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu {bloodRequest.RequestCode} đang ở trạng thái '{bloodRequest.Status}', chỉ có thể từ chối phiếu đang chờ xử lý (PENDING).");
        }

        var oldStatus = bloodRequest.Status;
        var now = DateTime.UtcNow;

        var reservedAllocations = bloodRequest.Allocations
            .Where(a => a.Status == AllocationStatus.RESERVED)
            .ToList();

        var releasedBags = new List<BloodBag>();
        foreach (var allocation in reservedAllocations)
        {
            if (allocation.Bag != null && allocation.Bag.Status == BagStatus.RESERVED)
            {
                allocation.Bag.Status = BagStatus.AVAILABLE;
                releasedBags.Add(allocation.Bag);
            }
        }

        if (reservedAllocations.Count > 0)
        {
            _context.BloodAllocations.RemoveRange(reservedAllocations);
        }

        foreach (var item in bloodRequest.Items)
        {
            item.AllocatedUnits = 0;
        }

        bloodRequest.Status = RequestStatus.REJECTED;
        bloodRequest.RejectionReason = reason;

        var auditLog = new AuditLog
        {
            TableName = "blood_requests",
            RecordId = bloodRequest.RequestId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                status = oldStatus.ToString(),
                reservedAllocationCount = reservedAllocations.Count
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = bloodRequest.Status.ToString(),
                rejectionReason = bloodRequest.RejectionReason,
                releasedBagIds = releasedBags.Select(b => b.BagId).ToList()
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Từ chối phiếu yêu cầu máu {bloodRequest.RequestCode}. Lý do: {reason}. Hoàn trả {releasedBags.Count} túi máu đã giữ chỗ.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        foreach (var bag in releasedBags)
        {
            _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
            {
                BagId = bag.BagId,
                FromStatus = BagStatus.RESERVED,
                ToStatus = BagStatus.AVAILABLE,
                PerformedById = userId,
                AuditLogId = auditLog.LogId,
                Notes = $"Hoàn trả giữ chỗ do từ chối phiếu {bloodRequest.RequestCode}",
                CreatedAt = now
            });
        }

        if (releasedBags.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return new RejectBloodRequestResponse
        {
            RequestId = bloodRequest.RequestId,
            RequestCode = bloodRequest.RequestCode,
            Status = bloodRequest.Status,
            CurrentStep = BloodRequestMappings.ResolveCurrentStep(bloodRequest.Status),
            RejectionReason = bloodRequest.RejectionReason,
            ReleasedBagCount = releasedBags.Count,
            RejectedAt = now
        };
    }

    private async Task EnsureActiveUserAsync(int userId, string errorMessage)
    {
        if (userId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, errorMessage);
        }

        var exists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId && u.IsActive);

        if (!exists)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Tài khoản thực hiện không tồn tại hoặc đã bị vô hiệu hóa.");
        }
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }
    }
}
