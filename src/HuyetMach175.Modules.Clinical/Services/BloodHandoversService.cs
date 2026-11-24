using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;
using HuyetMach175.Modules.Clinical.Mappings;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Services;

public class BloodHandoversService : IBloodHandoversService
{
    private readonly ApplicationDbContext _context;

    public BloodHandoversService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HandoverSessionDto> CreateHandoverSessionAsync(CreateHandoverSessionRequest request, int userId, string? ipAddress)
    {
        await BloodRequestProcessing.EnsureActiveUserAsync(_context, userId, "Thông tin kỹ thuật viên xuất kho không hợp lệ.");

        if (request.TimeoutMinutes < 3 || request.TimeoutMinutes > 5)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Thời gian chờ xác nhận phiên bàn giao phải từ 3 đến 5 phút.");
        }

        if (request.RequestId.HasValue && request.RequestId.Value <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu (requestId) không hợp lệ.");
        }

        var normalizedBarcodes = BloodHandoverProcessing.NormalizeAndValidateBarcodes(request.Barcodes, "barcodes");

        var now = DateTime.UtcNow;
        await BloodHandoverProcessing.ExpireOverdueSessionsAndClearTrackerAsync(_context, now);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var (bags, allocations, bloodRequest) = await BloodHandoverProcessing.LoadAndValidateReservedBatchAsync(
            _context,
            normalizedBarcodes,
            request.RequestId,
            now);

        var sessionCode = await BloodRequestProcessing.GenerateHandoverSessionCodeAsync(_context, now);
        var expiresAt = now.AddMinutes(request.TimeoutMinutes);

        var session = new BloodHandoverSession
        {
            SessionCode = sessionCode,
            RequestId = bloodRequest.RequestId,
            InitiatedBy = userId,
            Status = HandoverSessionStatus.PENDING,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            Items = allocations.Select(a => new BloodHandoverSessionItem
            {
                AllocationId = a.AllocationId,
                BagId = a.BagId
            }).ToList()
        };

        _context.BloodHandoverSessions.Add(session);
        await _context.SaveChangesAsync();

        _context.Notifications.Add(BloodHandoverProcessing.BuildHandoverSessionNotification(
            session,
            bloodRequest,
            bags.Count,
            request.TimeoutMinutes,
            now));

        _context.AuditLogs.Add(BloodHandoverProcessing.BuildAuditLog(
            "blood_handover_sessions",
            session.HandoverSessionId,
            ActionType.INSERT,
            null,
            new
            {
                sessionCode = session.SessionCode,
                status = session.Status.ToString(),
                requestId = bloodRequest.RequestId,
                requestCode = bloodRequest.RequestCode,
                timeoutMinutes = request.TimeoutMinutes,
                expiresAt,
                bagIds = bags.Select(b => b.BagId).ToList(),
                barcodes = bags.Select(b => b.Barcode).ToList()
            },
            userId,
            ipAddress,
            $"Khởi tạo phiên bàn giao {session.SessionCode} ({bags.Count} túi) cho phiếu {bloodRequest.RequestCode}.",
            now));

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BloodHandoverProcessing.LoadSessionDtoAsync(_context, session.HandoverSessionId, userId, now);
    }

    public async Task<List<HandoverSessionDto>> GetPendingHandoverSessionsAsync(PendingHandoverSessionFilterDto filter, int userId)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người dùng không hợp lệ.");

        if (filter.DoctorId.HasValue && filter.DoctorId.Value <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã bác sĩ (doctorId) không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        await BloodHandoverProcessing.ExpireOverdueSessionsAndClearTrackerAsync(_context, now);

        var hasHospitalWideAccess = await BloodRequestProcessing.HasHospitalWideClinicalAccessAsync(_context, userId, "BLOOD_REQUEST_APPROVE");

        var query = BloodHandoverProcessing.BuildSessionDetailQuery(_context)
            .AsNoTracking()
            .Where(s => s.Status == HandoverSessionStatus.PENDING && s.Request != null);

        if (!hasHospitalWideAccess)
        {
            var departmentId = currentUser.DepartmentId;
            query = query.Where(s => s.Request!.Doctor != null && s.Request.Doctor.DepartmentId == departmentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SessionCode))
        {
            var sessionKw = filter.SessionCode.Trim();
            query = query.Where(s => EF.Functions.ILike(s.SessionCode, $"%{sessionKw}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.RequestCode))
        {
            var requestKw = filter.RequestCode.Trim();
            query = query.Where(s => EF.Functions.ILike(s.Request!.RequestCode, $"%{requestKw}%"));
        }

        if (filter.DoctorId.HasValue)
        {
            var doctorId = filter.DoctorId.Value;
            query = query.Where(s => s.Request!.DoctorId == doctorId);
        }

        if (!string.IsNullOrWhiteSpace(filter.DoctorName))
        {
            var doctorNameKw = filter.DoctorName.Trim();
            query = query.Where(s => s.Request!.Doctor != null && EF.Functions.ILike(s.Request.Doctor.FullName, $"%{doctorNameKw}%"));
        }

        var sessions = await query.ToListAsync();

        return sessions
            .Select(s => s.ToDto(userId, now))
            .OrderByDescending(dto => dto.IsOwnRequest)
            .ThenBy(dto => BloodCompatibilityRules.GetUrgencyRank(dto.UrgencyLevel))
            .ThenBy(dto => dto.ExpiresAt)
            .ThenBy(dto => dto.HandoverSessionId)
            .ToList();
    }

    public async Task<HandoverSessionDto> GetHandoverSessionByIdAsync(int sessionId, int userId)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người dùng không hợp lệ.");

        if (sessionId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiên bàn giao (sessionId) không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        await BloodHandoverProcessing.ExpireOverdueSessionsAndClearTrackerAsync(_context, now);

        var session = await BloodHandoverProcessing.BuildSessionDetailQuery(_context)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.HandoverSessionId == sessionId);

        if (session == null || session.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiên bàn giao #{sessionId}.");
        }

        var hasHospitalWideAccess = await BloodRequestProcessing.HasHospitalWideClinicalAccessAsync(_context, userId, "BLOOD_REQUEST_APPROVE");
        if (!hasHospitalWideAccess)
        {
            BloodRequestProcessing.EnsureSameDepartmentAccess(
                currentUser,
                session.Request,
                "Bạn chỉ có quyền xem phiên bàn giao thuộc khoa của mình.");
        }

        return session.ToDto(userId, now);
    }

    public async Task<HandoverSessionDto> ConfirmHandoverSessionAsync(int sessionId, ConfirmHandoverSessionRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người nhận không hợp lệ.");

        if (sessionId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiên bàn giao (sessionId) không hợp lệ.");
        }

        var scannedBarcodes = BloodHandoverProcessing.NormalizeAndValidateBarcodes(request.ScannedBarcodes, "scannedBarcodes");

        var now = DateTime.UtcNow;
        await BloodHandoverProcessing.ExpireOverdueSessionsAndClearTrackerAsync(_context, now);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var session = await BloodHandoverProcessing.BuildSessionDetailQuery(_context)
            .Include(s => s.Request)
                .ThenInclude(r => r!.Items)
            .Include(s => s.Request)
                .ThenInclude(r => r!.Allocations)
            .FirstOrDefaultAsync(s => s.HandoverSessionId == sessionId);

        if (session == null || session.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiên bàn giao #{sessionId}.");
        }

        BloodRequestProcessing.EnsureSameDepartmentAccess(
            currentUser,
            session.Request,
            "Bạn chỉ có quyền xác nhận phiên bàn giao cho phiếu yêu cầu thuộc khoa của mình.");

        BloodHandoverProcessing.ValidateConfirmSessionStateAndBarcodes(session, scannedBarcodes, now);
        BloodHandoverProcessing.ValidateExpectedPatientCode(session.Request, userId, request.ExpectedPatientCode);

        var oldRequestStatus = session.Request.Status;
        var bagChanges = new List<(BloodBag Bag, int? OldLocationId)>();

        session.Status = HandoverSessionStatus.CONFIRMED;
        session.ConfirmedBy = userId;
        session.ConfirmedAt = now;

        foreach (var item in session.Items)
        {
            var bag = item.Bag!;
            var allocation = item.Allocation!;

            bagChanges.Add((bag, bag.LocationId));
            bag.Status = BagStatus.ISSUED;
            bag.LocationId = null;

            allocation.Status = AllocationStatus.COMPLETED;
            allocation.IssuedBy = session.InitiatedBy;
            allocation.IssuedAt = now;
            allocation.ReceivedBy = userId;
        }

        var allItemsAllocated = session.Request.Items.Count > 0 &&
                                session.Request.Items.All(i => i.AllocatedUnits >= i.RequestedUnits);
        var allAllocationsDone = session.Request.Allocations
            .All(a => a.Status == AllocationStatus.COMPLETED || a.Status == AllocationStatus.RETURNED);

        if (allItemsAllocated && allAllocationsDone)
        {
            session.Request.Status = RequestStatus.COMPLETED;
        }

        var auditLog = BloodHandoverProcessing.BuildAuditLog(
            "blood_handover_sessions",
            session.HandoverSessionId,
            ActionType.STATUS_CHANGE,
            new
            {
                sessionStatus = HandoverSessionStatus.PENDING.ToString(),
                requestStatus = oldRequestStatus.ToString(),
                bags = bagChanges.Select(bc => new
                {
                    bagId = bc.Bag.BagId,
                    bagStatus = BagStatus.RESERVED.ToString(),
                    locationId = bc.OldLocationId
                }).ToList()
            },
            new
            {
                sessionStatus = session.Status.ToString(),
                requestStatus = session.Request.Status.ToString(),
                confirmedBy = userId,
                confirmedAt = now,
                barcodes = session.Items.Select(i => i.Bag!.Barcode).ToList(),
                patientCode = session.Request.PatientCode
            },
            userId,
            ipAddress,
            $"Lâm sàng xác nhận bàn giao phiên {session.SessionCode} ({session.Items.Count} túi) cho phiếu {session.Request.RequestCode}.",
            now);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        foreach (var (bag, _) in bagChanges)
        {
            _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
            {
                BagId = bag.BagId,
                FromStatus = BagStatus.RESERVED,
                ToStatus = BagStatus.ISSUED,
                PerformedById = userId,
                AuditLogId = auditLog.LogId,
                Notes = $"Xuất kho và bàn giao trực tiếp qua phiên {session.SessionCode} (phiếu {session.Request.RequestCode})",
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BloodHandoverProcessing.LoadSessionDtoAsync(_context, session.HandoverSessionId, userId, now);
    }

    public async Task<HandoverSessionDto> CancelHandoverSessionAsync(int sessionId, CancelHandoverSessionRequest? request, int userId, string? ipAddress)
    {
        await BloodRequestProcessing.EnsureActiveUserAsync(_context, userId, "Thông tin kỹ thuật viên hủy phiên không hợp lệ.");

        if (sessionId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiên bàn giao (sessionId) không hợp lệ.");
        }

        var cancelReason = request?.CancelReason?.Trim();
        if (!string.IsNullOrWhiteSpace(cancelReason) && cancelReason.Length > 255)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do hủy phiên tối đa 255 ký tự.");
        }

        var now = DateTime.UtcNow;
        await BloodHandoverProcessing.ExpireOverdueSessionsAndClearTrackerAsync(_context, now);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var session = await BloodHandoverProcessing.BuildSessionDetailQuery(_context)
            .FirstOrDefaultAsync(s => s.HandoverSessionId == sessionId);

        if (session == null || session.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiên bàn giao #{sessionId}.");
        }

        if (session.Status == HandoverSessionStatus.CONFIRMED)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiên bàn giao {session.SessionCode} đã xác nhận hoàn tất, không thể hủy.");
        }

        if (session.Status != HandoverSessionStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiên bàn giao {session.SessionCode} đang ở trạng thái '{session.Status}', chỉ có thể hủy phiên đang chờ xác nhận (PENDING).");
        }

        session.Status = HandoverSessionStatus.CANCELLED;
        session.CancelledBy = userId;
        session.CancelledAt = now;
        session.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason;

        _context.AuditLogs.Add(BloodHandoverProcessing.BuildAuditLog(
            "blood_handover_sessions",
            session.HandoverSessionId,
            ActionType.STATUS_CHANGE,
            new { status = HandoverSessionStatus.PENDING.ToString() },
            new
            {
                status = session.Status.ToString(),
                cancelledBy = userId,
                cancelledAt = now,
                cancelReason = session.CancelReason
            },
            userId,
            ipAddress,
            string.IsNullOrWhiteSpace(session.CancelReason)
                ? $"Hủy sớm phiên bàn giao {session.SessionCode}; giữ nguyên giữ chỗ RESERVED cho các túi máu."
                : $"Hủy sớm phiên bàn giao {session.SessionCode} ({session.CancelReason}); giữ nguyên giữ chỗ RESERVED.",
            now));

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await BloodHandoverProcessing.LoadSessionDtoAsync(_context, session.HandoverSessionId, userId, now);
    }

    public async Task<TransfusionScanDto> ScanBloodBagForTransfusionAsync(string barcode, int userId)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người thực hiện không hợp lệ.");
        var (bag, allocation) = await BloodHandoverProcessing.LoadIssuedBagForTransfusionAsync(_context, barcode, currentUser, asNoTracking: true);
        return bag.ToTransfusionScanDto(allocation, userId, DateTime.UtcNow);
    }

    public async Task<ConfirmTransfusionResponse> ConfirmTransfusionAsync(ConfirmTransfusionRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người xác nhận truyền máu không hợp lệ.");

        var expectedPatientCode = request.ExpectedPatientCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expectedPatientCode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã người bệnh đối soát (expectedPatientCode) không được để trống.");
        }

        if (request.AllocationId.HasValue && request.AllocationId.Value <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phân bổ (allocationId) không hợp lệ.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var (bag, allocation) = await BloodHandoverProcessing.LoadIssuedBagForTransfusionAsync(_context, request.Barcode, currentUser, asNoTracking: false);
        var now = DateTime.UtcNow;

        if (bag.ExpiredAt <= now)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đã hết hạn sử dụng vào {bag.ExpiredAt:dd/MM/yyyy HH:mm}, không được phép truyền cho người bệnh.");
        }

        if (request.AllocationId.HasValue && request.AllocationId.Value != allocation.AllocationId)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Mã phân bổ #{request.AllocationId.Value} không khớp với lượt cấp phát của túi máu {bag.Barcode}.");
        }

        if (!string.Equals(allocation.Request!.PatientCode, expectedPatientCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Mã người bệnh đối soát '{expectedPatientCode}' không khớp với bệnh nhân trên phiếu {allocation.Request.RequestCode}.");
        }

        var hasPendingReturn = await _context.BloodReturns
            .AsNoTracking()
            .AnyAsync(r => r.AllocationId == allocation.AllocationId && r.FinalAction == ReturnFinalAction.PENDING_INSPECTION);

        if (hasPendingReturn)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi máu {bag.Barcode} đang có phiếu hoàn trả chờ kiểm định.");
        }

        bag.Status = BagStatus.TRANSFUSED;

        var auditLog = BloodHandoverProcessing.BuildAuditLog(
            "blood_bags",
            bag.BagId,
            ActionType.STATUS_CHANGE,
            new
            {
                bagStatus = BagStatus.ISSUED.ToString(),
                allocationId = allocation.AllocationId,
                requestCode = allocation.Request!.RequestCode
            },
            new
            {
                bagStatus = bag.Status.ToString(),
                allocationId = allocation.AllocationId,
                requestCode = allocation.Request.RequestCode,
                patientCode = allocation.Request.PatientCode,
                patientName = allocation.Request.PatientName,
                transfusedAt = now
            },
            userId,
            ipAddress,
            $"Lâm sàng xác nhận bắt đầu truyền máu túi {bag.Barcode} cho người bệnh {allocation.Request.PatientName} ({allocation.Request.PatientCode}).",
            now);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = bag.BagId,
            FromStatus = BagStatus.ISSUED,
            ToStatus = BagStatus.TRANSFUSED,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = $"Xác nhận truyền máu tại giường cho người bệnh {allocation.Request.PatientName} ({allocation.Request.PatientCode}) - Phiếu {allocation.Request.RequestCode}",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return bag.ToConfirmTransfusionResponse(allocation, currentUser, now);
    }

    public async Task<List<ReservedHandoverRequestDto>> GetReservedRequestsForHandoverAsync(string? search, string? urgencyLevel)
    {
        var now = DateTime.UtcNow;
        await BloodHandoverProcessing.ExpireOverdueSessionsAndClearTrackerAsync(_context, now);

        UrgencyLevel? parsedUrgency = null;
        if (!string.IsNullOrWhiteSpace(urgencyLevel))
        {
            if (!Enum.TryParse<UrgencyLevel>(urgencyLevel.Trim(), true, out var urgency))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Mức độ khẩn cấp '{urgencyLevel}' không hợp lệ.");
            }
            parsedUrgency = urgency;
        }

        var allocationsQuery = _context.BloodAllocations
            .AsNoTracking()
            .Include(a => a.ReservedByUser)
            .Include(a => a.Bag)
                .ThenInclude(b => b!.ComponentType)
            .Include(a => a.Bag)
                .ThenInclude(b => b!.Location)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.Department)
            .Where(a =>
                a.Status == AllocationStatus.RESERVED &&
                a.Bag != null &&
                a.Bag.Status == BagStatus.RESERVED &&
                a.Request != null &&
                (a.Request.Status == RequestStatus.PENDING || a.Request.Status == RequestStatus.READY));

        if (parsedUrgency.HasValue)
        {
            allocationsQuery = allocationsQuery.Where(a => a.Request!.UrgencyLevel == parsedUrgency.Value);
        }

        var allocations = await allocationsQuery.ToListAsync();

        var allocationIds = allocations.Select(a => a.AllocationId).ToList();
        var pendingSessionItems = allocationIds.Count == 0
            ? new Dictionary<int, (int SessionId, string SessionCode)>()
            : await _context.BloodHandoverSessionItems
                .AsNoTracking()
                .Include(i => i.HandoverSession)
                .Where(i =>
                    allocationIds.Contains(i.AllocationId) &&
                    i.HandoverSession != null &&
                    i.HandoverSession.Status == HandoverSessionStatus.PENDING &&
                    i.HandoverSession.ExpiresAt > now)
                .ToDictionaryAsync(
                    i => i.AllocationId,
                    i => (i.HandoverSessionId, i.HandoverSession!.SessionCode));

        var keyword = search?.Trim();

        return allocations
            .GroupBy(a => a.RequestId)
            .Select(group =>
            {
                var req = group.First().Request!;
                var bags = group
                    .OrderBy(a => a.Bag!.ExpiredAt)
                    .ThenBy(a => a.BagId)
                    .Select(a => a.ToReservedHandoverBagDto(
                        pendingSessionItems.TryGetValue(a.AllocationId, out var sessionInfo) ? sessionInfo : null,
                        now))
                    .ToList();

                var inPendingCount = bags.Count(b => b.ActiveSessionId.HasValue);

                return new ReservedHandoverRequestDto
                {
                    RequestId = req.RequestId,
                    RequestCode = req.RequestCode,
                    RequestStatus = req.Status,
                    UrgencyLevel = req.UrgencyLevel,
                    PatientCode = req.PatientCode,
                    PatientName = req.PatientName,
                    PatientBloodType = BloodCompatibilityRules.FormatBloodGroup(req.PatientBloodType, req.PatientRh),
                    DepartmentName = req.Doctor?.Department?.DepartmentName,
                    DoctorName = req.Doctor?.FullName,
                    CreatedAt = req.CreatedAt,
                    TotalReservedBags = bags.Count,
                    ReadyForSessionCount = bags.Count - inPendingCount,
                    InPendingSessionCount = inPendingCount,
                    Bags = bags
                };
            })
            .Where(dto => string.IsNullOrWhiteSpace(keyword) ||
                dto.RequestCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                dto.PatientCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                dto.PatientName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (dto.DepartmentName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                dto.Bags.Any(b => b.Barcode.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(dto => BloodCompatibilityRules.GetUrgencyRank(dto.UrgencyLevel))
            .ThenBy(dto => dto.CreatedAt)
            .ThenBy(dto => dto.RequestId)
            .ToList();
    }
}
