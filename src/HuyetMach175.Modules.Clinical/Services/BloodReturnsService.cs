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

    public async Task<CreateBloodReturnResponse> CreateBloodReturnAsync(CreateBloodReturnRequest request, Guid userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người lập phiếu hoàn trả không hợp lệ.");
        var (returnReason, _) = BloodReturnProcessing.ValidateCreateReturnInput(request);

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
        var previousRestockCount = await BloodReturnProcessing.CountPreviousRestocksAsync(_context, bag.BagId);
        var failedChecklistFlags = BloodReturnProcessing.GetFailedCreateChecklistCriteria(request.Checklist, bag.ComponentType);
        failedChecklistFlags.AddRange(BloodReturnProcessing.EvaluateRestockEligibility(bag, previousRestockCount, now));
        var isEligibleForRestockEvaluation = failedChecklistFlags.Count == 0;
        var returnCode = await BloodReturnProcessing.GenerateReturnCodeAsync(_context, now);

        var bloodReturn = new BloodReturn
        {
            ReturnCode = returnCode,
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
            ? $"Lâm sàng lập phiếu hoàn trả {bloodReturn.ReturnCode} cho túi máu {bag.Barcode}."
            : $"Lâm sàng lập phiếu hoàn trả {bloodReturn.ReturnCode} cho túi máu {bag.Barcode} (Cảnh báo không đạt điều kiện tái nhập: {string.Join("; ", failedChecklistFlags)}).";

        var auditLog = BloodHandoverProcessing.BuildAuditLog(
            "blood_returns",
            bloodReturn.ReturnId,
            ActionType.INSERT,
            new { bagId = bag.BagId, bagStatus = oldBagStatus.ToString() },
            new
            {
                returnId = bloodReturn.ReturnId,
                returnCode = bloodReturn.ReturnCode,
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
            ? $"Phong tỏa túi máu do lập phiếu hoàn trả {bloodReturn.ReturnCode}: {returnReason}"
            : $"Phong tỏa túi máu do lập phiếu hoàn trả {bloodReturn.ReturnCode} [Cảnh báo tiêu hủy bắt buộc: {string.Join("; ", failedChecklistFlags)}]: {returnReason}";

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

    public async Task<CancelBloodReturnResponse> CancelBloodReturnAsync(Guid returnId, CancelBloodReturnRequest? request, Guid userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người hủy phiếu hoàn trả không hợp lệ.");

        if (returnId == Guid.Empty)
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
            ? $"Lâm sàng hủy phiếu hoàn trả {bloodReturn.ReturnCode} lập nhầm; khôi phục túi {bag.Barcode} về ISSUED."
            : $"Lâm sàng hủy phiếu hoàn trả {bloodReturn.ReturnCode} ({appliedCancelReason}); khôi phục túi {bag.Barcode} về ISSUED.";

        var auditLog = BloodHandoverProcessing.BuildAuditLog(
            "blood_returns",
            bloodReturn.ReturnId,
            ActionType.STATUS_CHANGE,
            new { returnCode = bloodReturn.ReturnCode, finalAction = ReturnFinalAction.PENDING_INSPECTION.ToString(), bagStatus = BagStatus.LOCKED.ToString() },
            new
            {
                returnCode = bloodReturn.ReturnCode,
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
            ? $"Khôi phục trạng thái túi máu do hủy phiếu hoàn trả {bloodReturn.ReturnCode}"
            : $"Khôi phục trạng thái túi máu do hủy phiếu hoàn trả {bloodReturn.ReturnCode}: {appliedCancelReason}";

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

    public async Task<PagedResult<PendingBloodReturnDto>> GetPendingBloodReturnsAsync(PendingBloodReturnFilterDto filter, Guid userId)
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
                EF.Functions.ILike(br.ReturnCode, $"%{kw}%") ||
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

        var bagIds = returns.Select(r => r.Allocation!.BagId).Distinct().ToList();
        var returnIds = returns.Select(r => r.ReturnId).ToList();
        var restockCounts = await BloodReturnProcessing.CountPreviousRestocksByBagIdsAsync(_context, bagIds);
        var clinicalWarningsMap = await BloodReturnProcessing.GetClinicalChecklistWarningsAsync(_context, returnIds);

        return new PagedResult<PendingBloodReturnDto>
        {
            Items = returns.Select(br => br.ToPendingDto(
                restockCounts.GetValueOrDefault(br.Allocation!.BagId, 0),
                clinicalWarningsMap.GetValueOrDefault(br.ReturnId) ?? new List<string>(),
                now)).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<ReturnInspectionDto> ScanBloodReturnByBarcodeAsync(string barcode, Guid userId)
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
            .OrderByDescending(br => br.ProcessedAt)
            .ThenByDescending(br => br.ReturnId)
            .FirstOrDefaultAsync();

        if (bloodReturn?.Allocation?.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Túi máu {bag.Barcode} chưa có phiếu hoàn trả nào trong hệ thống.");
        }

        if (bloodReturn.FinalAction != ReturnFinalAction.PENDING_INSPECTION)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi máu {bag.Barcode} (phiếu hoàn trả {bloodReturn.ReturnCode}) đã được xử lý trước đó với kết quả '{bloodReturn.FinalAction}'.");
        }

        if (bag.Status != BagStatus.LOCKED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đang ở trạng thái '{bag.Status}', không ở trạng thái phong tỏa (LOCKED) để kiểm định hoàn trả.");
        }

        var (previousLocationId, previousLocationName) = await BloodReturnProcessing.ResolvePreviousLocationAsync(
            _context,
            bloodReturn.Allocation.AllocationId,
            bag.BagId);

        var previousRestockCount = await BloodReturnProcessing.CountPreviousRestocksAsync(_context, bag.BagId);
        var clinicalWarningsMap = await BloodReturnProcessing.GetClinicalChecklistWarningsAsync(_context, new[] { bloodReturn.ReturnId });
        var clinicalWarnings = clinicalWarningsMap.GetValueOrDefault(bloodReturn.ReturnId) ?? new List<string>();

        return bloodReturn.ToInspectionDto(previousLocationId, previousLocationName, previousRestockCount, clinicalWarnings, DateTime.UtcNow);
    }

    public async Task<ProcessBloodReturnResponse> ProcessBloodReturnAsync(Guid returnId, ProcessBloodReturnRequest request, Guid userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người kiểm định không hợp lệ.");

        if (returnId == Guid.Empty)
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
            var previousRestockCount = await BloodReturnProcessing.CountPreviousRestocksAsync(_context, bag.BagId);
            targetLocation = await BloodReturnProcessing.ValidateRestockTargetLocationAsync(
                _context,
                bag,
                previousRestockCount,
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
            targetLocation = await BloodReturnProcessing.ResolveDiscardHoldLocationAsync(_context, request.NewStorageLocationId);

            bloodReturn.Qualified = false;
            bloodReturn.FinalAction = ReturnFinalAction.DISCARD;
            bag.Status = BagStatus.LOCKED;
            bag.LocationId = targetLocation?.LocationId;
            bag.DiscardReason = appliedDiscardReason;

            responseMessage = targetLocation != null
                ? $"Từ chối tái nhập kho: Túi máu {bag.Barcode} được giữ trạng thái tạm khóa (LOCKED), đưa vào khu vực chờ hủy '{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}' để lập biên bản tiêu hủy."
                : $"Từ chối tái nhập kho: Túi máu {bag.Barcode} được giữ trạng thái tạm khóa (LOCKED) để chờ lập biên bản tiêu hủy.";
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
