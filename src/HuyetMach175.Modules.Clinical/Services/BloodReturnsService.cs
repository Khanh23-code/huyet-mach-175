using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Services;

public class BloodReturnsService : IBloodReturnsService
{
    private readonly ApplicationDbContext _context;

    public BloodReturnsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateBloodReturnResponse> CreateBloodReturnAsync(CreateBloodReturnRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người lập phiếu hoàn trả không hợp lệ.");

        if (request.AllocationId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phân bổ (allocationId) không hợp lệ.");
        }

        var returnReason = request.ReturnReason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(returnReason))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do hoàn trả (returnReason) không được để trống.");
        }

        if (returnReason.Length > 255)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do hoàn trả không được vượt quá 255 ký tự.");
        }

        ValidateCreateChecklist(request.Checklist);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var allocation = await _context.BloodAllocations
            .Include(a => a.Bag)
                .ThenInclude(b => b!.ComponentType)
            .Include(a => a.BloodReturn)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
            .FirstOrDefaultAsync(a => a.AllocationId == request.AllocationId);

        if (allocation == null || allocation.Bag == null || allocation.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lượt phân bổ túi máu #{request.AllocationId}.");
        }

        BloodRequestProcessing.EnsureSameDepartmentAccess(
            currentUser,
            allocation.Request,
            "Bạn chỉ có quyền lập phiếu hoàn trả cho túi máu thuộc khoa của mình.");

        if (allocation.BloodReturn != null)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Lượt phân bổ #{allocation.AllocationId} (túi {allocation.Bag.Barcode}) đã được lập phiếu hoàn trả trước đó (#{allocation.BloodReturn.ReturnId}).");
        }

        if (allocation.Bag.Status != BagStatus.ISSUED ||
            (allocation.Status != AllocationStatus.ISSUED && allocation.Status != AllocationStatus.COMPLETED))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {allocation.Bag.Barcode} đang ở trạng thái '{allocation.Bag.Status}' (phân bổ '{allocation.Status}'), chỉ có thể lập phiếu hoàn trả cho túi đã xuất kho.");
        }

        var now = DateTime.UtcNow;
        var oldBagStatus = allocation.Bag.Status;

        var bloodReturn = new BloodReturn
        {
            AllocationId = allocation.AllocationId,
            ReturnedBy = userId,
            ReceivedBy = userId,
            ReturnReason = returnReason,
            Qualified = false,
            FinalAction = ReturnFinalAction.PENDING_INSPECTION,
            ProcessedAt = now
        };

        allocation.Bag.Status = BagStatus.LOCKED;

        _context.BloodReturns.Add(bloodReturn);
        await _context.SaveChangesAsync();

        var auditLog = new AuditLog
        {
            TableName = "blood_returns",
            RecordId = bloodReturn.ReturnId,
            ActionType = ActionType.INSERT,
            OldState = JsonSerializer.Serialize(new
            {
                bagId = allocation.Bag.BagId,
                bagStatus = oldBagStatus.ToString()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                returnId = bloodReturn.ReturnId,
                allocationId = allocation.AllocationId,
                bagId = allocation.Bag.BagId,
                barcode = allocation.Bag.Barcode,
                bagStatus = allocation.Bag.Status.ToString(),
                finalAction = bloodReturn.FinalAction.ToString(),
                qualified = bloodReturn.Qualified,
                returnReason,
                checklist = request.Checklist
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Lâm sàng lập phiếu hoàn trả #{bloodReturn.ReturnId} cho túi máu {allocation.Bag.Barcode}.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = allocation.Bag.BagId,
            FromStatus = BagStatus.ISSUED,
            ToStatus = BagStatus.LOCKED,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = $"Phong tỏa túi máu do lập phiếu hoàn trả #{bloodReturn.ReturnId}: {returnReason}",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CreateBloodReturnResponse
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = allocation.AllocationId,
            RequestId = allocation.Request.RequestId,
            RequestCode = allocation.Request.RequestCode,
            PatientCode = allocation.Request.PatientCode,
            PatientName = allocation.Request.PatientName,
            BagId = allocation.Bag.BagId,
            Barcode = allocation.Bag.Barcode,
            ComponentName = allocation.Bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(allocation.Bag.BloodType, allocation.Bag.RhFactor),
            ReturnReason = bloodReturn.ReturnReason,
            Qualified = bloodReturn.Qualified,
            FinalAction = bloodReturn.FinalAction,
            BagStatus = allocation.Bag.Status,
            ReturnedById = userId,
            ReturnedByName = currentUser.FullName,
            ProcessedAt = now
        };
    }

    public async Task<PagedResult<PendingBloodReturnDto>> GetPendingBloodReturnsAsync(PendingBloodReturnFilterDto filter, int userId)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người dùng không hợp lệ.");
        BloodRequestProcessing.ValidatePagination(filter.PageNumber, filter.PageSize);

        var canViewAll = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.IsActive)
            .SelectMany(u => u.UserRoles)
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .AnyAsync(ur =>
                ur.Role!.RoleCode == "SYS" ||
                ur.Role.RoleCode == "MGT" ||
                ur.Role.RoleCode == "BBNK" ||
                ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.PermissionCode == "BLOOD_RETURN_APPROVE"));

        var query = _context.BloodReturns
            .AsNoTracking()
            .Include(br => br.ReturnedByUser)
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Bag)
                    .ThenInclude(b => b!.ComponentType)
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Request)
                    .ThenInclude(r => r!.Doctor)
                        .ThenInclude(d => d!.Department)
            .Where(br =>
                br.FinalAction == ReturnFinalAction.PENDING_INSPECTION &&
                br.Allocation != null &&
                br.Allocation.Bag != null &&
                br.Allocation.Request != null);

        if (!canViewAll)
        {
            var departmentId = currentUser.DepartmentId;
            query = query.Where(br =>
                br.Allocation!.Request!.Doctor != null &&
                br.Allocation.Request.Doctor.DepartmentId == departmentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(br =>
                EF.Functions.ILike(br.Allocation!.Bag!.Barcode, $"%{kw}%") ||
                EF.Functions.ILike(br.Allocation.Request!.RequestCode, $"%{kw}%") ||
                EF.Functions.ILike(br.Allocation.Request.PatientCode, $"%{kw}%") ||
                EF.Functions.ILike(br.Allocation.Request.PatientName, $"%{kw}%") ||
                EF.Functions.ILike(br.ReturnReason, $"%{kw}%"));
        }

        var totalCount = await query.CountAsync();
        var now = DateTime.UtcNow;

        var returns = await query
            .OrderBy(br => br.ProcessedAt)
            .ThenBy(br => br.ReturnId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = returns.Select(br => new PendingBloodReturnDto
        {
            ReturnId = br.ReturnId,
            AllocationId = br.AllocationId,
            BagId = br.Allocation!.Bag!.BagId,
            Barcode = br.Allocation.Bag.Barcode,
            ComponentCode = br.Allocation.Bag.ComponentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = br.Allocation.Bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(br.Allocation.Bag.BloodType, br.Allocation.Bag.RhFactor),
            VolumeMl = br.Allocation.Bag.VolumeMl,
            ExpiredAt = br.Allocation.Bag.ExpiredAt,
            IsExpired = br.Allocation.Bag.ExpiredAt <= now,
            RequestId = br.Allocation.Request!.RequestId,
            RequestCode = br.Allocation.Request.RequestCode,
            PatientCode = br.Allocation.Request.PatientCode,
            PatientName = br.Allocation.Request.PatientName,
            DepartmentName = br.Allocation.Request.Doctor?.Department?.DepartmentName,
            ReturnedById = br.ReturnedBy,
            ReturnedByName = br.ReturnedByUser?.FullName,
            ReturnReason = br.ReturnReason,
            FinalAction = br.FinalAction,
            BagStatus = br.Allocation.Bag.Status,
            ProcessedAt = br.ProcessedAt
        }).ToList();

        return new PagedResult<PendingBloodReturnDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<ReturnInspectionDto> ScanBloodReturnByBarcodeAsync(string barcode, int userId)
    {
        await BloodRequestProcessing.EnsureActiveUserAsync(_context, userId, "Thông tin người kiểm định không hợp lệ.");

        var rawBarcode = barcode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawBarcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã vạch túi máu (barcode) không được để trống.");
        }

        var normalizedBarcode = rawBarcode.ToLower();
        var bag = await _context.BloodBags
            .AsNoTracking()
            .Include(b => b.ComponentType)
            .FirstOrDefaultAsync(b => b.Barcode.ToLower() == normalizedBarcode);

        if (bag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu có mã vạch '{rawBarcode}'.");
        }

        var bloodReturn = await _context.BloodReturns
            .AsNoTracking()
            .Include(br => br.ReturnedByUser)
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Request)
                    .ThenInclude(r => r!.Doctor)
                        .ThenInclude(d => d!.Department)
            .Where(br => br.Allocation != null && br.Allocation.BagId == bag.BagId)
            .OrderByDescending(br => br.ReturnId)
            .FirstOrDefaultAsync();

        if (bloodReturn == null || bloodReturn.Allocation == null || bloodReturn.Allocation.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Túi máu {bag.Barcode} chưa có phiếu hoàn trả nào trong hệ thống.");
        }

        if (bloodReturn.FinalAction != ReturnFinalAction.PENDING_INSPECTION)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi máu {bag.Barcode} (phiếu hoàn trả #{bloodReturn.ReturnId}) đã được xử lý trước đó với kết quả '{bloodReturn.FinalAction}'.");
        }

        if (bag.Status != BagStatus.LOCKED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đang ở trạng thái '{bag.Status}', không ở trạng thái phong tỏa (LOCKED) để kiểm định hoàn trả.");
        }

        var (previousLocationId, previousLocationName) = await ResolvePreviousLocationAsync(bloodReturn.Allocation.AllocationId);
        var now = DateTime.UtcNow;

        return new ReturnInspectionDto
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = bloodReturn.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentTypeId = bag.ComponentTypeId,
            ComponentCode = bag.ComponentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            VolumeMl = bag.VolumeMl,
            CollectedAt = bag.CollectedAt,
            ExpiredAt = bag.ExpiredAt,
            IsExpired = bag.ExpiredAt <= now,
            BagStatus = bag.Status,
            PreviousLocationId = previousLocationId,
            PreviousLocationName = previousLocationName,
            RequestId = bloodReturn.Allocation.Request.RequestId,
            RequestCode = bloodReturn.Allocation.Request.RequestCode,
            PatientCode = bloodReturn.Allocation.Request.PatientCode,
            PatientName = bloodReturn.Allocation.Request.PatientName,
            DepartmentName = bloodReturn.Allocation.Request.Doctor?.Department?.DepartmentName,
            DoctorName = bloodReturn.Allocation.Request.Doctor?.FullName,
            ReturnedById = bloodReturn.ReturnedBy,
            ReturnedByName = bloodReturn.ReturnedByUser?.FullName,
            ReturnReason = bloodReturn.ReturnReason,
            Qualified = bloodReturn.Qualified,
            FinalAction = bloodReturn.FinalAction,
            ReturnedAt = bloodReturn.ProcessedAt
        };
    }

    public async Task<ProcessBloodReturnResponse> ProcessBloodReturnAsync(int returnId, ProcessBloodReturnRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người kiểm định không hợp lệ.");

        if (returnId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu hoàn trả (returnId) không hợp lệ.");
        }

        if (request.Action != ReturnFinalAction.RESTOCK && request.Action != ReturnFinalAction.DISCARD)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Quyết định xử lý hoàn trả (action) chỉ được phép là 'RESTOCK' hoặc 'DISCARD'.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var bloodReturn = await _context.BloodReturns
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Bag)
                    .ThenInclude(b => b!.ComponentType)
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Request)
            .FirstOrDefaultAsync(br => br.ReturnId == returnId);

        if (bloodReturn == null || bloodReturn.Allocation == null || bloodReturn.Allocation.Bag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu hoàn trả #{returnId}.");
        }

        if (bloodReturn.FinalAction != ReturnFinalAction.PENDING_INSPECTION)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiếu hoàn trả #{returnId} đã được xử lý trước đó với kết quả '{bloodReturn.FinalAction}'.");
        }

        var bag = bloodReturn.Allocation.Bag;
        if (bag.Status != BagStatus.LOCKED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đang ở trạng thái '{bag.Status}', chỉ có thể kiểm định túi đang phong tỏa (LOCKED).");
        }

        var now = DateTime.UtcNow;
        var oldFinalAction = bloodReturn.FinalAction;
        var oldQualified = bloodReturn.Qualified;
        var oldBagStatus = bag.Status;
        var oldLocationId = bag.LocationId;
        var oldAllocationStatus = bloodReturn.Allocation.Status;

        bloodReturn.ReceivedBy = userId;
        bloodReturn.ProcessedAt = now;
        bloodReturn.Allocation.Status = AllocationStatus.RETURNED;

        StorageLocation? targetLocation = null;
        string? appliedDiscardReason = null;
        string responseMessage;

        if (request.Action == ReturnFinalAction.RESTOCK)
        {
            if (bag.ExpiredAt <= now)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đã hết hạn sử dụng vào {bag.ExpiredAt:dd/MM/yyyy HH:mm}, không thể tái nhập kho. Vui lòng chọn 'DISCARD'.");
            }

            if (request.Checklist == null)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Bảng kiểm 5 điều kiện bảo quản (checklist) là bắt buộc khi nhập lại kho.");
            }

            var failedCriteria = GetFailedInspectionCriteria(request.Checklist);
            if (failedCriteria.Count > 0)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu không đạt đủ 5 điều kiện tái nhập kho: {string.Join("; ", failedCriteria)}.");
            }

            if (!request.NewStorageLocationId.HasValue || request.NewStorageLocationId.Value <= 0)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Vị trí ngăn tủ lưu trữ mới (newStorageLocationId) là bắt buộc khi nhập lại kho.");
            }

            targetLocation = await _context.StorageLocations
                .FirstOrDefaultAsync(l => l.LocationId == request.NewStorageLocationId.Value);

            if (targetLocation == null)
            {
                throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với ID {request.NewStorageLocationId.Value}.");
            }

            if (!targetLocation.IsActive)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}' đang ngừng hoạt động.");
            }

            if (targetLocation.IsFull)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}' hiện đã đầy.");
            }

            bloodReturn.Qualified = true;
            bloodReturn.FinalAction = ReturnFinalAction.RESTOCK;

            bag.Status = BagStatus.AVAILABLE;
            bag.LocationId = targetLocation.LocationId;

            responseMessage = $"Kiểm định đạt chuẩn: Đã tái nhập kho túi máu {bag.Barcode} vào '{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}'.";
        }
        else
        {
            appliedDiscardReason = ResolveDiscardReason(request, bloodReturn.ReturnReason);

            bloodReturn.Qualified = false;
            bloodReturn.FinalAction = ReturnFinalAction.DISCARD;

            bag.Status = BagStatus.DISCARDED;
            bag.LocationId = null;
            bag.DiscardReason = appliedDiscardReason;

            responseMessage = $"Từ chối nhập kho: Đã chuyển túi máu {bag.Barcode} sang trạng thái tiêu hủy (DISCARDED).";
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_returns",
            RecordId = bloodReturn.ReturnId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                finalAction = oldFinalAction.ToString(),
                qualified = oldQualified,
                bagStatus = oldBagStatus.ToString(),
                locationId = oldLocationId,
                allocationStatus = oldAllocationStatus.ToString()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                finalAction = bloodReturn.FinalAction.ToString(),
                qualified = bloodReturn.Qualified,
                bagStatus = bag.Status.ToString(),
                locationId = bag.LocationId,
                allocationStatus = bloodReturn.Allocation.Status.ToString(),
                discardReason = appliedDiscardReason,
                checklist = request.Checklist
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = responseMessage,
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = bag.BagId,
            FromStatus = BagStatus.LOCKED,
            ToStatus = bag.Status,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = responseMessage,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ProcessBloodReturnResponse
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = bloodReturn.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            Qualified = bloodReturn.Qualified,
            FinalAction = bloodReturn.FinalAction,
            BagStatus = bag.Status,
            AllocationStatus = bloodReturn.Allocation.Status,
            NewLocationId = targetLocation?.LocationId,
            NewLocationName = targetLocation != null ? $"{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}" : null,
            DiscardReason = appliedDiscardReason,
            InspectedById = userId,
            InspectedByName = currentUser.FullName,
            ProcessedAt = now,
            Message = responseMessage
        };
    }

    private async Task<(int? LocationId, string? LocationName)> ResolvePreviousLocationAsync(int allocationId)
    {
        var oldStates = await _context.AuditLogs
            .AsNoTracking()
            .Where(a =>
                a.TableName == "blood_allocations" &&
                a.RecordId == allocationId &&
                a.ActionType == ActionType.STATUS_CHANGE &&
                a.OldState != null)
            .OrderByDescending(a => a.LogId)
            .Select(a => a.OldState)
            .ToListAsync();

        int? previousLocationId = null;
        foreach (var oldState in oldStates)
        {
            if (string.IsNullOrWhiteSpace(oldState))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(oldState);
                if (doc.RootElement.TryGetProperty("locationId", out var locProp) &&
                    locProp.ValueKind == JsonValueKind.Number &&
                    locProp.TryGetInt32(out var locId))
                {
                    previousLocationId = locId;
                    break;
                }
            }
            catch (JsonException)
            {
            }
        }

        if (!previousLocationId.HasValue)
        {
            return (null, null);
        }

        var location = await _context.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LocationId == previousLocationId.Value);

        var locationName = location != null
            ? $"{location.RefrigeratorName} - {location.ShelfNumber}"
            : null;

        return (previousLocationId, locationName);
    }

    private static string ResolveDiscardReason(ProcessBloodReturnRequest request, string returnReason)
    {
        var explicitReason = request.DiscardReason?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitReason))
        {
            return explicitReason.Length <= 255 ? explicitReason : explicitReason[..255];
        }

        if (request.Checklist != null)
        {
            var failedCriteria = GetFailedInspectionCriteria(request.Checklist);
            if (failedCriteria.Count > 0)
            {
                var summary = $"Không đạt kiểm định hoàn trả: {string.Join("; ", failedCriteria)}";
                return summary.Length <= 255 ? summary : summary[..255];
            }
        }

        var fallback = $"Không đạt kiểm định tái nhập kho ({returnReason})";
        return fallback.Length <= 255 ? fallback : fallback[..255];
    }

    private static void ValidateCreateChecklist(BloodReturnChecklistDto? checklist)
    {
        if (checklist == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Bảng kiểm điều kiện hoàn trả (checklist) không được để trống.");
        }

        var failedCriteria = new List<string>();
        if (!checklist.IsUnusedFullVolume)
        {
            failedCriteria.Add("Chưa sử dụng, còn nguyên thể tích");
        }

        if (!checklist.IsBagIntact)
        {
            failedCriteria.Add("Túi máu còn nguyên vẹn");
        }

        if (!checklist.IsLabelClear)
        {
            failedCriteria.Add("Nhãn mác còn rõ ràng");
        }

        if (!checklist.IsStoredUnder30Mins)
        {
            failedCriteria.Add("Thời gian ngoài tủ lạnh dưới 30 phút");
        }

        if (failedCriteria.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu không đạt đủ 4 điều kiện hoàn trả bắt buộc: {string.Join("; ", failedCriteria)}.");
        }
    }

    private static List<string> GetFailedInspectionCriteria(BloodReturnInspectionChecklistDto checklist)
    {
        var failed = new List<string>();
        if (!checklist.IsBagIntact)
        {
            failed.Add("Túi máu còn nguyên vẹn");
        }

        if (!checklist.IsLabelClear)
        {
            failed.Add("Nhãn mác còn rõ ràng");
        }

        if (!checklist.IsTempInRange)
        {
            failed.Add("Nhiệt độ bảo quản đạt chuẩn");
        }

        if (!checklist.IsOutsideTimeValid)
        {
            failed.Add("Thời gian ngoài tủ lạnh hợp lệ");
        }

        if (!checklist.IsUnusedFullVolume)
        {
            failed.Add("Chưa sử dụng, còn nguyên thể tích");
        }

        return failed;
    }
}
