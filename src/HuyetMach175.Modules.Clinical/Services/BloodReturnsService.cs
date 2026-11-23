using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Mappings;
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
        var (returnReason, failedChecklistFlags) = BloodReturnProcessing.ValidateCreateReturnInput(request);
        var isEligibleForRestockEvaluation = failedChecklistFlags.Count == 0;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var allocation = await _context.BloodAllocations
            .Include(a => a.Bag)
                .ThenInclude(b => b!.ComponentType)
            .Include(a => a.BloodReturns)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
            .FirstOrDefaultAsync(a => a.AllocationId == request.AllocationId);

        BloodReturnProcessing.ValidateAllocationForReturn(allocation, request.AllocationId, currentUser);

        var now = DateTime.UtcNow;
        var bag = allocation!.Bag!;
        var oldBagStatus = bag.Status;

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

        bag.Status = BagStatus.LOCKED;

        _context.BloodReturns.Add(bloodReturn);
        await _context.SaveChangesAsync();

        var changeReason = isEligibleForRestockEvaluation
            ? $"Lâm sàng lập phiếu hoàn trả #{bloodReturn.ReturnId} cho túi máu {bag.Barcode}."
            : $"Lâm sàng lập phiếu hoàn trả #{bloodReturn.ReturnId} cho túi máu {bag.Barcode} (Cảnh báo không đạt điều kiện tái nhập: {string.Join("; ", failedChecklistFlags)}).";

        var auditLog = BloodHandoverProcessing.BuildAuditLog(
            "blood_returns",
            bloodReturn.ReturnId,
            ActionType.INSERT,
            new { bagId = bag.BagId, bagStatus = oldBagStatus.ToString() },
            new
            {
                returnId = bloodReturn.ReturnId,
                allocationId = allocation.AllocationId,
                bagId = bag.BagId,
                barcode = bag.Barcode,
                bagStatus = bag.Status.ToString(),
                finalAction = bloodReturn.FinalAction.ToString(),
                qualified = bloodReturn.Qualified,
                returnReason,
                isEligibleForRestockEvaluation,
                failedChecklistFlags,
                checklist = request.Checklist
            },
            userId,
            ipAddress,
            BloodReturnProcessing.Truncate255(changeReason),
            now);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        var statusNote = isEligibleForRestockEvaluation
            ? $"Phong tỏa túi máu do lập phiếu hoàn trả #{bloodReturn.ReturnId}: {returnReason}"
            : $"Phong tỏa túi máu do lập phiếu hoàn trả #{bloodReturn.ReturnId} [Cảnh báo tiêu hủy bắt buộc: {string.Join("; ", failedChecklistFlags)}]: {returnReason}";

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = bag.BagId,
            FromStatus = BagStatus.ISSUED,
            ToStatus = BagStatus.LOCKED,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = BloodReturnProcessing.Truncate255(statusNote),
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return bloodReturn.ToCreateResponse(allocation, currentUser, isEligibleForRestockEvaluation, failedChecklistFlags);
    }

    public async Task<CancelBloodReturnResponse> CancelBloodReturnAsync(int returnId, CancelBloodReturnRequest? request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người hủy phiếu hoàn trả không hợp lệ.");

        if (returnId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu hoàn trả (returnId) không hợp lệ.");
        }

        var cancelReason = request?.CancelReason?.Trim();
        if (!string.IsNullOrWhiteSpace(cancelReason) && cancelReason.Length > 255)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do hủy phiếu hoàn trả tối đa 255 ký tự.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var bloodReturn = await _context.BloodReturns
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Bag)
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Request)
                    .ThenInclude(r => r!.Doctor)
            .FirstOrDefaultAsync(br => br.ReturnId == returnId);

        if (bloodReturn?.Allocation?.Request != null)
        {
            BloodRequestProcessing.EnsureSameDepartmentAccess(
                currentUser,
                bloodReturn.Allocation.Request,
                "Bạn chỉ có quyền hủy phiếu hoàn trả thuộc khoa của mình.");
        }

        BloodReturnProcessing.ValidatePendingReturnForInspection(
            bloodReturn,
            returnId,
            bloodReturn?.Allocation?.Bag != null
                ? $"Túi máu {bloodReturn.Allocation.Bag.Barcode} đang ở trạng thái '{bloodReturn.Allocation.Bag.Status}', không hợp lệ để khôi phục về ISSUED."
                : "Túi máu không ở trạng thái LOCKED.");

        var bag = bloodReturn!.Allocation!.Bag!;
        var hasActiveDisposal = await _context.BloodDisposalItems
            .AsNoTracking()
            .Include(i => i.DisposalRecord)
            .AnyAsync(i =>
                i.BagId == bag.BagId &&
                i.DisposalRecord != null &&
                (i.DisposalRecord.Status == DisposalStatus.PENDING_APPROVAL || i.DisposalRecord.Status == DisposalStatus.APPROVED));

        if (hasActiveDisposal)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi máu {bag.Barcode} đang nằm trong biên bản tiêu hủy chờ xử lý, không thể hủy phiếu hoàn trả.");
        }

        var now = DateTime.UtcNow;
        var appliedCancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason;

        bloodReturn.FinalAction = ReturnFinalAction.CANCELLED;
        bloodReturn.Qualified = false;
        bloodReturn.ProcessedAt = now;
        bag.Status = BagStatus.ISSUED;

        var changeReason = string.IsNullOrWhiteSpace(appliedCancelReason)
            ? $"Lâm sàng hủy phiếu hoàn trả #{bloodReturn.ReturnId} lập nhầm; khôi phục túi {bag.Barcode} về ISSUED."
            : $"Lâm sàng hủy phiếu hoàn trả #{bloodReturn.ReturnId} ({appliedCancelReason}); khôi phục túi {bag.Barcode} về ISSUED.";

        var auditLog = BloodHandoverProcessing.BuildAuditLog(
            "blood_returns",
            bloodReturn.ReturnId,
            ActionType.STATUS_CHANGE,
            new { finalAction = ReturnFinalAction.PENDING_INSPECTION.ToString(), bagStatus = BagStatus.LOCKED.ToString() },
            new
            {
                finalAction = bloodReturn.FinalAction.ToString(),
                bagStatus = bag.Status.ToString(),
                cancelledBy = userId,
                cancelledAt = now,
                cancelReason = appliedCancelReason
            },
            userId,
            ipAddress,
            BloodReturnProcessing.Truncate255(changeReason),
            now);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        var noteText = string.IsNullOrWhiteSpace(appliedCancelReason)
            ? $"Khôi phục trạng thái túi máu do hủy phiếu hoàn trả #{bloodReturn.ReturnId}"
            : $"Khôi phục trạng thái túi máu do hủy phiếu hoàn trả #{bloodReturn.ReturnId}: {appliedCancelReason}";

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = bag.BagId,
            FromStatus = BagStatus.LOCKED,
            ToStatus = BagStatus.ISSUED,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = BloodReturnProcessing.Truncate255(noteText),
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return bloodReturn.ToCancelResponse(bag, currentUser, appliedCancelReason);
    }

    public async Task<PagedResult<PendingBloodReturnDto>> GetPendingBloodReturnsAsync(PendingBloodReturnFilterDto filter, int userId)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người dùng không hợp lệ.");
        BloodRequestProcessing.ValidatePagination(filter.PageNumber, filter.PageSize);

        var canViewAll = await BloodRequestProcessing.HasHospitalWideClinicalAccessAsync(_context, userId, "BLOOD_RETURN_APPROVE");

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

        return new PagedResult<PendingBloodReturnDto>
        {
            Items = returns.Select(br => br.ToPendingDto(now)).ToList(),
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
                .ThenInclude(a => a!.Bag)
                    .ThenInclude(b => b!.ComponentType)
            .Include(br => br.Allocation)
                .ThenInclude(a => a!.Request)
                    .ThenInclude(r => r!.Doctor)
                        .ThenInclude(d => d!.Department)
            .Where(br =>
                br.Allocation != null &&
                br.Allocation.BagId == bag.BagId &&
                br.FinalAction != ReturnFinalAction.CANCELLED)
            .OrderByDescending(br => br.ReturnId)
            .FirstOrDefaultAsync();

        if (bloodReturn?.Allocation?.Request == null)
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

        var (previousLocationId, previousLocationName) = await BloodReturnProcessing.ResolvePreviousLocationAsync(
            _context,
            bloodReturn.Allocation.AllocationId,
            bag.BagId);

        return bloodReturn.ToInspectionDto(previousLocationId, previousLocationName, DateTime.UtcNow);
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

        BloodReturnProcessing.ValidatePendingReturnForInspection(
            bloodReturn,
            returnId,
            bloodReturn?.Allocation?.Bag != null
                ? $"Túi máu {bloodReturn.Allocation.Bag.Barcode} đang ở trạng thái '{bloodReturn.Allocation.Bag.Status}', chỉ có thể kiểm định túi đang phong tỏa (LOCKED)."
                : "Túi máu không ở trạng thái LOCKED.");

        var bag = bloodReturn!.Allocation!.Bag!;
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
            targetLocation = await BloodReturnProcessing.ValidateRestockTargetLocationAsync(
                _context,
                bag,
                request.Checklist,
                request.NewStorageLocationId,
                now);

            bloodReturn.Qualified = true;
            bloodReturn.FinalAction = ReturnFinalAction.RESTOCK;
            bag.Status = BagStatus.AVAILABLE;
            bag.LocationId = targetLocation.LocationId;

            responseMessage = $"Kiểm định đạt chuẩn: Đã tái nhập kho túi máu {bag.Barcode} vào '{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}'.";
        }
        else
        {
            appliedDiscardReason = BloodReturnProcessing.ResolveDiscardReason(request, bloodReturn.ReturnReason);

            bloodReturn.Qualified = false;
            bloodReturn.FinalAction = ReturnFinalAction.DISCARD;
            bag.Status = BagStatus.DISCARDED;
            bag.LocationId = null;
            bag.DiscardReason = appliedDiscardReason;

            responseMessage = $"Từ chối nhập kho: Đã chuyển túi máu {bag.Barcode} sang trạng thái tiêu hủy (DISCARDED).";
        }

        var auditLog = BloodHandoverProcessing.BuildAuditLog(
            "blood_returns",
            bloodReturn.ReturnId,
            ActionType.STATUS_CHANGE,
            new
            {
                finalAction = oldFinalAction.ToString(),
                qualified = oldQualified,
                bagStatus = oldBagStatus.ToString(),
                locationId = oldLocationId,
                allocationStatus = oldAllocationStatus.ToString()
            },
            new
            {
                finalAction = bloodReturn.FinalAction.ToString(),
                qualified = bloodReturn.Qualified,
                bagStatus = bag.Status.ToString(),
                locationId = bag.LocationId,
                allocationStatus = bloodReturn.Allocation.Status.ToString(),
                discardReason = appliedDiscardReason,
                checklist = request.Checklist
            },
            userId,
            ipAddress,
            BloodReturnProcessing.Truncate255(responseMessage),
            now);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = bag.BagId,
            FromStatus = BagStatus.LOCKED,
            ToStatus = bag.Status,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = BloodReturnProcessing.Truncate255(responseMessage),
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return bloodReturn.ToProcessResponse(bag, targetLocation, appliedDiscardReason, currentUser, responseMessage);
    }
}
