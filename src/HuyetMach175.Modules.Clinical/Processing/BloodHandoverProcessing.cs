using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.BackgroundServices;
using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;
using HuyetMach175.Modules.Clinical.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Processing;

public static class BloodHandoverProcessing
{
    public static async Task ExpireOverdueSessionsAndClearTrackerAsync(ApplicationDbContext context, DateTime nowUtc)
    {
        if (await HandoverSessionExpirationWorker.ExpireOverdueSessionsAsync(context, nowUtc) > 0)
        {
            context.ChangeTracker.Clear();
        }
    }

    public static IQueryable<BloodHandoverSession> BuildSessionDetailQuery(ApplicationDbContext context)
    {
        return context.BloodHandoverSessions
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

    public static async Task<HandoverSessionDto> LoadSessionDtoAsync(
        ApplicationDbContext context,
        Guid sessionId,
        Guid currentUserId,
        DateTime nowUtc)
    {
        var session = await BuildSessionDetailQuery(context)
            .AsNoTracking()
            .FirstAsync(s => s.HandoverSessionId == sessionId);

        return session.ToDto(currentUserId, nowUtc);
    }

    public static List<string> NormalizeAndValidateBarcodes(List<string>? rawBarcodes, string fieldName)
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

    public static async Task<(List<BloodBag> Bags, List<BloodAllocation> Allocations, BloodRequest Request)> LoadAndValidateReservedBatchAsync(
        ApplicationDbContext context,
        List<string> normalizedBarcodes,
        Guid? expectedRequestId,
        DateTime nowUtc)
    {
        var loweredBarcodes = normalizedBarcodes.Select(b => b.ToLowerInvariant()).ToList();
        var bags = await context.BloodBags
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

        var expiredBags = bags.Where(b => b.ExpiredAt <= nowUtc).ToList();
        if (expiredBags.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Không thể bàn giao túi máu đã hết hạn sử dụng: {string.Join(", ", expiredBags.Select(b => b.Barcode))}.");
        }

        var bagIds = bags.Select(b => b.BagId).ToList();
        var allocations = await context.BloodAllocations
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

        if (expectedRequestId.HasValue && expectedRequestId.Value != bloodRequest.RequestId)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Các túi máu được chọn thuộc phiếu {bloodRequest.RequestCode}, không khớp với requestId #{expectedRequestId.Value}.");
        }

        if (bloodRequest.Status != RequestStatus.READY && bloodRequest.Status != RequestStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu {bloodRequest.RequestCode} đang ở trạng thái '{bloodRequest.Status}', không hợp lệ để mở phiên bàn giao.");
        }

        var allocationIds = allocations.Select(a => a.AllocationId).ToList();
        var conflictingItems = await context.BloodHandoverSessionItems
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

        return (bags, allocations, bloodRequest);
    }

    public static void ValidateExpectedPatientCode(BloodRequest bloodRequest, Guid userId, string? rawExpectedPatientCode)
    {
        var expectedPatientCode = rawExpectedPatientCode?.Trim() ?? string.Empty;
        var isOwnRequest = bloodRequest.DoctorId == userId;

        if (!isOwnRequest && string.IsNullOrWhiteSpace(expectedPatientCode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Khi nhận hộ phiếu yêu cầu của bác sĩ khác, bắt buộc nhập mã người bệnh đối soát (expectedPatientCode).");
        }

        if (!string.IsNullOrWhiteSpace(expectedPatientCode) &&
            !string.Equals(bloodRequest.PatientCode, expectedPatientCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Mã người bệnh đối soát '{expectedPatientCode}' không khớp với bệnh nhân trên phiếu {bloodRequest.RequestCode}.");
        }
    }

    public static void ValidateConfirmSessionStateAndBarcodes(
        BloodHandoverSession session,
        List<string> scannedBarcodes,
        DateTime nowUtc)
    {
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

        var expiredBags = session.Items.Where(i => i.Bag!.ExpiredAt <= nowUtc).Select(i => i.Bag!.Barcode).ToList();
        if (expiredBags.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Không thể xác nhận nhận túi máu đã hết hạn sử dụng: {string.Join(", ", expiredBags)}.");
        }
    }

    public static async Task<(BloodBag Bag, BloodAllocation Allocation)> LoadIssuedBagForTransfusionAsync(
        ApplicationDbContext context,
        string? rawBarcode,
        User currentUser,
        bool asNoTracking)
    {
        var barcode = rawBarcode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã vạch túi máu (barcode) không được để trống.");
        }

        var normalizedBarcode = barcode.ToLowerInvariant();
        IQueryable<BloodBag> bagQuery = context.BloodBags
            .Include(b => b.ComponentType);

        if (asNoTracking)
        {
            bagQuery = bagQuery.AsNoTracking();
        }

        var bag = await bagQuery.FirstOrDefaultAsync(b => b.Barcode.ToLower() == normalizedBarcode);
        if (bag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu có mã vạch '{barcode}'.");
        }

        if (bag.Status == BagStatus.TRANSFUSED)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi máu {bag.Barcode} đã được xác nhận truyền máu trước đó.");
        }

        if (bag.Status != BagStatus.ISSUED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đang ở trạng thái '{bag.Status}', chỉ có thể xác nhận truyền máu cho túi đã xuất kho cho Lâm sàng (ISSUED).");
        }

        var allocation = await context.BloodAllocations
            .AsNoTracking()
            .Include(a => a.ReceivedByUser)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.Department)
            .Where(a => a.BagId == bag.BagId &&
                        (a.Status == AllocationStatus.COMPLETED || a.Status == AllocationStatus.ISSUED))
            .OrderByDescending(a => a.AllocationId)
            .FirstOrDefaultAsync();

        if (allocation == null || allocation.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lượt cấp phát hợp lệ cho túi máu {bag.Barcode}.");
        }

        BloodRequestProcessing.EnsureSameDepartmentAccess(
            currentUser,
            allocation.Request,
            "Bạn chỉ có quyền thao tác truyền máu cho túi máu thuộc khoa của mình.");

        return (bag, allocation);
    }

    public static Notification? BuildHandoverSessionNotification(
        BloodHandoverSession session,
        BloodRequest bloodRequest,
        int bagCount,
        int timeoutMinutes,
        DateTime nowUtc)
    {
        if (bloodRequest.DoctorId is not Guid doctorId || doctorId == Guid.Empty)
        {
            return null;
        }

        return new Notification
        {
            RecipientUserId = doctorId,
            Title = $"Mời quét xác nhận nhận máu tại quầy ({session.SessionCode})",
            Content = $"Ngân hàng máu đã mở phiên bàn giao {session.SessionCode} gồm {bagCount} túi máu cho phiếu {bloodRequest.RequestCode}. Vui lòng quét nhận trong {timeoutMinutes} phút.",
            NotificationType = "HANDOVER_SESSION_CREATED",
            SourceModule = "Clinical",
            DeduplicationKey = $"HANDOVER_SESSION_CREATED:{session.HandoverSessionId}",
            ResourceType = "HANDOVER_SESSION",
            ResourceId = session.HandoverSessionId,
            ActionPath = $"/clinical/handovers/{session.HandoverSessionId}",
            Priority = bloodRequest.UrgencyLevel is UrgencyLevel.EMERGENCY or UrgencyLevel.URGENT
                ? NotificationPriority.Critical
                : NotificationPriority.Normal,
            ExpiresAt = session.ExpiresAt,
            IsRead = false,
            CreatedAt = nowUtc
        };
    }

    public static AuditLog BuildAuditLog(
        string tableName,
        Guid recordId,
        ActionType actionType,
        object? oldState,
        object newState,
        Guid userId,
        string? ipAddress,
        string changeReason,
        DateTime nowUtc)
    {
        return new AuditLog
        {
            TableName = tableName,
            RecordId = recordId,
            ActionType = actionType,
            OldState = oldState != null ? JsonSerializer.Serialize(oldState) : null,
            NewState = JsonSerializer.Serialize(newState),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = changeReason,
            CreatedAt = nowUtc
        };
    }
}
