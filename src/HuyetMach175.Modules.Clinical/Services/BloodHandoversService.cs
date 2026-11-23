using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.BackgroundServices;
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

        var normalizedBarcodes = NormalizeAndValidateBarcodes(request.Barcodes, "barcodes");

        var now = DateTime.UtcNow;
        if (await HandoverSessionExpirationWorker.ExpireOverdueSessionsAsync(_context, now) > 0)
        {
            _context.ChangeTracker.Clear();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var loweredBarcodes = normalizedBarcodes.Select(b => b.ToLowerInvariant()).ToList();
        var bags = await _context.BloodBags
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .Where(b => loweredBarcodes.Contains(b.Barcode.ToLower()))
            .ToListAsync();

        var foundSet = bags.Select(b => b.Barcode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingBarcodes = normalizedBarcodes.Where(b => !foundSet.Contains(b)).ToList();
        if (missingBarcodes.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu với mã vạch: {string.Join(", ", missingBarcodes)}.");
        }

        var nonReservedBags = bags.Where(b => b.Status != BagStatus.RESERVED).ToList();
        if (nonReservedBags.Count > 0)
        {
            var details = nonReservedBags.Select(b => $"{b.Barcode} ({b.Status})");
            throw new HttpException(StatusCodes.Status400BadRequest, $"Chỉ có thể mở phiên bàn giao cho các túi máu đang được giữ chỗ (RESERVED). Túi không hợp lệ: {string.Join(", ", details)}.");
        }

        var expiredBags = bags.Where(b => b.ExpiredAt <= now).ToList();
        if (expiredBags.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Không thể bàn giao túi máu đã hết hạn sử dụng: {string.Join(", ", expiredBags.Select(b => b.Barcode))}.");
        }

        var bagIds = bags.Select(b => b.BagId).ToList();
        var allocations = await _context.BloodAllocations
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.Department)
            .Where(a => bagIds.Contains(a.BagId) && a.Status == AllocationStatus.RESERVED)
            .ToListAsync();

        var allocationByBagId = allocations.ToDictionary(a => a.BagId);
        var unallocatedBarcodes = bags.Where(b => !allocationByBagId.ContainsKey(b.BagId)).Select(b => b.Barcode).ToList();
        if (unallocatedBarcodes.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy thông tin giữ chỗ (RESERVED) cho túi máu: {string.Join(", ", unallocatedBarcodes)}.");
        }

        var distinctRequestIds = allocations.Select(a => a.RequestId).Distinct().ToList();
        if (distinctRequestIds.Count != 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Một phiên bàn giao theo lô chỉ được gom các túi máu thuộc cùng một phiếu yêu cầu.");
        }

        var bloodRequest = allocations[0].Request;
        if (bloodRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy phiếu yêu cầu máu tương ứng.");
        }

        if (request.RequestId.HasValue && request.RequestId.Value != bloodRequest.RequestId)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Các túi máu được chọn thuộc phiếu {bloodRequest.RequestCode}, không khớp với requestId #{request.RequestId.Value}.");
        }

        if (bloodRequest.Status != RequestStatus.READY && bloodRequest.Status != RequestStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu {bloodRequest.RequestCode} đang ở trạng thái '{bloodRequest.Status}', không hợp lệ để mở phiên bàn giao.");
        }

        var allocationIds = allocations.Select(a => a.AllocationId).ToList();
        var conflictingItems = await _context.BloodHandoverSessionItems
            .AsNoTracking()
            .Include(i => i.HandoverSession)
            .Include(i => i.Bag)
            .Where(i =>
                allocationIds.Contains(i.AllocationId) &&
                i.HandoverSession != null &&
                i.HandoverSession.Status == HandoverSessionStatus.PENDING)
            .ToListAsync();

        if (conflictingItems.Count > 0)
        {
            var conflictDetails = conflictingItems.Select(i => $"{i.Bag?.Barcode} (phiên {i.HandoverSession?.SessionCode})");
            throw new HttpException(StatusCodes.Status409Conflict, $"Một hoặc nhiều túi máu đang nằm trong phiên bàn giao chờ xác nhận khác: {string.Join(", ", conflictDetails)}.");
        }

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

        _context.Notifications.Add(new Notification
        {
            RecipientUserId = bloodRequest.DoctorId,
            TargetDepartmentId = bloodRequest.Doctor?.DepartmentId,
            Title = $"Mời quét xác nhận nhận máu tại quầy ({session.SessionCode})",
            Message = $"Ngân hàng máu đã khởi tạo phiên bàn giao {session.SessionCode} gồm {bags.Count} túi máu cho phiếu {bloodRequest.RequestCode} (BN: {bloodRequest.PatientName} - {bloodRequest.PatientCode}). Vui lòng quét xác nhận trong {request.TimeoutMinutes} phút.",
            Type = bloodRequest.UrgencyLevel is UrgencyLevel.EMERGENCY or UrgencyLevel.URGENT
                ? NotificationType.URGENT_REQUEST
                : NotificationType.INFO,
            ReferenceType = NotificationReferenceType.HANDOVER_SESSION,
            ReferenceId = session.HandoverSessionId,
            IsRead = false,
            CreatedAt = now
        });

        _context.AuditLogs.Add(new AuditLog
        {
            TableName = "blood_handover_sessions",
            RecordId = session.HandoverSessionId,
            ActionType = ActionType.INSERT,
            OldState = null,
            NewState = JsonSerializer.Serialize(new
            {
                sessionCode = session.SessionCode,
                status = session.Status.ToString(),
                requestId = bloodRequest.RequestId,
                requestCode = bloodRequest.RequestCode,
                timeoutMinutes = request.TimeoutMinutes,
                expiresAt,
                bagIds,
                barcodes = bags.Select(b => b.Barcode).ToList()
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Khởi tạo phiên bàn giao {session.SessionCode} ({bags.Count} túi) cho phiếu {bloodRequest.RequestCode}.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var createdSession = await BuildSessionDetailQuery()
            .AsNoTracking()
            .FirstAsync(s => s.HandoverSessionId == session.HandoverSessionId);

        return MapToDto(createdSession, userId, now);
    }

    public async Task<List<HandoverSessionDto>> GetPendingHandoverSessionsAsync(PendingHandoverSessionFilterDto filter, int userId)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người dùng không hợp lệ.");

        if (filter.DoctorId.HasValue && filter.DoctorId.Value <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã bác sĩ (doctorId) không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        if (await HandoverSessionExpirationWorker.ExpireOverdueSessionsAsync(_context, now) > 0)
        {
            _context.ChangeTracker.Clear();
        }

        var hasHospitalWideAccess = await BloodRequestProcessing.HasHospitalWideClinicalAccessAsync(_context, userId, "BLOOD_REQUEST_APPROVE");

        var query = BuildSessionDetailQuery()
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
            .Select(s => MapToDto(s, userId, now))
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
        if (await HandoverSessionExpirationWorker.ExpireOverdueSessionsAsync(_context, now) > 0)
        {
            _context.ChangeTracker.Clear();
        }

        var session = await BuildSessionDetailQuery()
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

        return MapToDto(session, userId, now);
    }

    public async Task<HandoverSessionDto> ConfirmHandoverSessionAsync(int sessionId, ConfirmHandoverSessionRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người nhận không hợp lệ.");

        if (sessionId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiên bàn giao (sessionId) không hợp lệ.");
        }

        var scannedBarcodes = NormalizeAndValidateBarcodes(request.ScannedBarcodes, "scannedBarcodes");
        var expectedPatientCode = request.ExpectedPatientCode?.Trim() ?? string.Empty;

        var now = DateTime.UtcNow;
        if (await HandoverSessionExpirationWorker.ExpireOverdueSessionsAsync(_context, now) > 0)
        {
            _context.ChangeTracker.Clear();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var session = await BuildSessionDetailQuery()
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

        if (session.Status == HandoverSessionStatus.CONFIRMED)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiên bàn giao {session.SessionCode} đã được xác nhận hoàn tất trước đó.");
        }

        if (session.Status == HandoverSessionStatus.EXPIRED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiên bàn giao {session.SessionCode} đã hết hạn chờ xác nhận. Toàn bộ túi máu vẫn đang giữ chỗ (RESERVED), vui lòng đề nghị Ngân hàng máu mở phiên bàn giao mới.");
        }

        if (session.Status != HandoverSessionStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiên bàn giao {session.SessionCode} đang ở trạng thái '{session.Status}', không thể xác nhận.");
        }

        var isOwnRequest = session.Request.DoctorId == userId;
        if (!isOwnRequest && string.IsNullOrWhiteSpace(expectedPatientCode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Khi nhận hộ phiếu yêu cầu của bác sĩ khác, bắt buộc nhập mã người bệnh đối soát (expectedPatientCode).");
        }

        if (!string.IsNullOrWhiteSpace(expectedPatientCode) &&
            !string.Equals(session.Request.PatientCode, expectedPatientCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Mã người bệnh đối soát '{expectedPatientCode}' không khớp với bệnh nhân trên phiếu {session.Request.RequestCode}.");
        }

        var sessionBarcodes = session.Items
            .Where(i => i.Bag != null)
            .Select(i => i.Bag!.Barcode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scannedSet = scannedBarcodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingFromScan = sessionBarcodes.Where(b => !scannedSet.Contains(b)).ToList();
        var unexpectedInScan = scannedBarcodes.Where(b => !sessionBarcodes.Contains(b)).ToList();

        if (missingFromScan.Count > 0 || unexpectedInScan.Count > 0)
        {
            var errorParts = new List<string>();
            if (missingFromScan.Count > 0)
            {
                errorParts.Add($"còn thiếu túi [{string.Join(", ", missingFromScan)}]");
            }
            if (unexpectedInScan.Count > 0)
            {
                errorParts.Add($"dư túi không thuộc phiên [{string.Join(", ", unexpectedInScan)}]");
            }

            throw new HttpException(StatusCodes.Status400BadRequest, $"Danh sách mã vạch quét xác nhận chưa khớp 100% với phiên {session.SessionCode}: {string.Join("; ", errorParts)}.");
        }

        var invalidItems = session.Items
            .Where(i => i.Bag == null || i.Allocation == null || i.Bag.Status != BagStatus.RESERVED || i.Allocation.Status != AllocationStatus.RESERVED)
            .ToList();

        if (invalidItems.Count > 0)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Một hoặc nhiều túi máu trong phiên {session.SessionCode} không còn ở trạng thái giữ chỗ (RESERVED).");
        }

        var expiredBags = session.Items.Where(i => i.Bag!.ExpiredAt <= now).Select(i => i.Bag!.Barcode).ToList();
        if (expiredBags.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Không thể xác nhận nhận túi máu đã hết hạn sử dụng: {string.Join(", ", expiredBags)}.");
        }

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

        var auditLog = new AuditLog
        {
            TableName = "blood_handover_sessions",
            RecordId = session.HandoverSessionId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                sessionStatus = HandoverSessionStatus.PENDING.ToString(),
                requestStatus = oldRequestStatus.ToString(),
                bags = bagChanges.Select(bc => new
                {
                    bagId = bc.Bag.BagId,
                    bagStatus = BagStatus.RESERVED.ToString(),
                    locationId = bc.OldLocationId
                }).ToList()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                sessionStatus = session.Status.ToString(),
                requestStatus = session.Request.Status.ToString(),
                confirmedBy = userId,
                confirmedAt = now,
                barcodes = session.Items.Select(i => i.Bag!.Barcode).ToList(),
                patientCode = session.Request.PatientCode
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Lâm sàng xác nhận bàn giao phiên {session.SessionCode} ({session.Items.Count} túi) cho phiếu {session.Request.RequestCode}.",
            CreatedAt = now
        };

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

        var updatedSession = await BuildSessionDetailQuery()
            .AsNoTracking()
            .FirstAsync(s => s.HandoverSessionId == session.HandoverSessionId);

        return MapToDto(updatedSession, userId, now);
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
        if (await HandoverSessionExpirationWorker.ExpireOverdueSessionsAsync(_context, now) > 0)
        {
            _context.ChangeTracker.Clear();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var session = await BuildSessionDetailQuery()
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

        _context.AuditLogs.Add(new AuditLog
        {
            TableName = "blood_handover_sessions",
            RecordId = session.HandoverSessionId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                status = HandoverSessionStatus.PENDING.ToString()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = session.Status.ToString(),
                cancelledBy = userId,
                cancelledAt = now,
                cancelReason = session.CancelReason
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = string.IsNullOrWhiteSpace(session.CancelReason)
                ? $"Hủy sớm phiên bàn giao {session.SessionCode}; giữ nguyên giữ chỗ RESERVED cho các túi máu."
                : $"Hủy sớm phiên bàn giao {session.SessionCode} ({session.CancelReason}); giữ nguyên giữ chỗ RESERVED.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var updatedSession = await BuildSessionDetailQuery()
            .AsNoTracking()
            .FirstAsync(s => s.HandoverSessionId == session.HandoverSessionId);

        return MapToDto(updatedSession, userId, now);
    }

    private IQueryable<BloodHandoverSession> BuildSessionDetailQuery()
    {
        return _context.BloodHandoverSessions
            .Include(s => s.InitiatedByUser)
            .Include(s => s.ConfirmedByUser)
            .Include(s => s.CancelledByUser)
            .Include(s => s.Request)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.Department)
            .Include(s => s.Items)
                .ThenInclude(i => i.Allocation)
            .Include(s => s.Items)
                .ThenInclude(i => i.Bag)
                    .ThenInclude(b => b!.ComponentType)
            .Include(s => s.Items)
                .ThenInclude(i => i.Bag)
                    .ThenInclude(b => b!.Location);
    }

    private static List<string> NormalizeAndValidateBarcodes(List<string>? rawBarcodes, string fieldName)
    {
        if (rawBarcodes == null || rawBarcodes.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Danh sách mã vạch ({fieldName}) không được để trống.");
        }

        var trimmed = rawBarcodes
            .Select(b => b?.Trim() ?? string.Empty)
            .ToList();

        if (trimmed.Any(string.IsNullOrWhiteSpace))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Danh sách ({fieldName}) chứa mã vạch rỗng.");
        }

        var duplicates = trimmed
            .GroupBy(b => b, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Danh sách ({fieldName}) bị trùng lặp mã vạch: {string.Join(", ", duplicates)}.");
        }

        return trimmed;
    }

    private static HandoverSessionDto MapToDto(BloodHandoverSession session, int currentUserId, DateTime nowUtc)
    {
        var remainingSeconds = session.Status == HandoverSessionStatus.PENDING
            ? Math.Max(0, (int)Math.Ceiling((session.ExpiresAt - nowUtc).TotalSeconds))
            : 0;

        return new HandoverSessionDto
        {
            HandoverSessionId = session.HandoverSessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            RemainingSeconds = remainingSeconds,
            ConfirmedAt = session.ConfirmedAt,
            CancelledAt = session.CancelledAt,
            CancelReason = session.CancelReason,
            InitiatedById = session.InitiatedBy,
            InitiatedByName = session.InitiatedByUser?.FullName,
            ConfirmedById = session.ConfirmedBy,
            ConfirmedByName = session.ConfirmedByUser?.FullName,
            CancelledById = session.CancelledBy,
            CancelledByName = session.CancelledByUser?.FullName,
            RequestId = session.RequestId,
            RequestCode = session.Request?.RequestCode ?? string.Empty,
            DoctorId = session.Request?.DoctorId,
            DoctorName = session.Request?.Doctor?.FullName,
            DepartmentName = session.Request?.Doctor?.Department?.DepartmentName,
            IsOwnRequest = session.Request?.DoctorId == currentUserId,
            UrgencyLevel = session.Request?.UrgencyLevel ?? UrgencyLevel.ROUTINE,
            PatientCode = session.Request?.PatientCode ?? string.Empty,
            PatientName = session.Request?.PatientName ?? string.Empty,
            PatientBloodType = session.Request != null
                ? BloodCompatibilityRules.FormatBloodGroup(session.Request.PatientBloodType, session.Request.PatientRh)
                : string.Empty,
            RequestStatus = session.Request?.Status ?? RequestStatus.PENDING,
            CurrentStep = session.Request != null ? BloodRequestMappings.ResolveCurrentStep(session.Request.Status) : 0,
            Items = session.Items
                .OrderBy(i => i.ItemId)
                .Select(i => new HandoverSessionItemDto
                {
                    AllocationId = i.AllocationId,
                    BagId = i.BagId,
                    Barcode = i.Bag?.Barcode ?? string.Empty,
                    ComponentTypeId = i.Bag?.ComponentTypeId ?? 0,
                    ComponentCode = i.Bag?.ComponentType?.TypeCode ?? ComponentCode.WB,
                    ComponentName = i.Bag?.ComponentType?.TypeName ?? string.Empty,
                    BloodType = i.Bag != null
                        ? BloodCompatibilityRules.FormatBloodGroup(i.Bag.BloodType, i.Bag.RhFactor)
                        : string.Empty,
                    VolumeMl = i.Bag?.VolumeMl ?? 0,
                    ExpiredAt = i.Bag?.ExpiredAt ?? DateTime.MinValue,
                    BagStatus = i.Bag?.Status ?? BagStatus.RESERVED,
                    LocationName = i.Bag?.Location != null
                        ? $"{i.Bag.Location.RefrigeratorName} - {i.Bag.Location.ShelfNumber}"
                        : null
                })
                .ToList()
        };
    }
}
