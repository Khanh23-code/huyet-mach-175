using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Api.Data.Seeders;

public static class NotificationSeeder
{
    public static async Task SeedNotificationDataAsync(ApplicationDbContext context)
    {
        if (await context.Notifications.AnyAsync())
        {
            return;
        }

        var doctor = await context.Users.FirstOrDefaultAsync(u => u.Username == "doctor");
        var bloodbank = await context.Users.FirstOrDefaultAsync(u => u.Username == "bloodbank");
        var manager = await context.Users.FirstOrDefaultAsync(u => u.Username == "manager");

        if (doctor == null || bloodbank == null || manager == null)
        {
            return;
        }

        var emergencyRequest = await context.BloodRequests
            .FirstOrDefaultAsync(r => r.RequestCode == "YC-SEED-002");
        var readyRequest = await context.BloodRequests
            .FirstOrDefaultAsync(r => r.RequestCode == "YC-SEED-004");
        var pendingHandover = await context.BloodHandoverSessions
            .FirstOrDefaultAsync(s => s.SessionCode == "BG-SEED-001");
        var pendingReturn = await context.BloodReturns
            .Include(r => r.Allocation)
            .ThenInclude(a => a!.Bag)
            .FirstOrDefaultAsync(r => r.ReturnCode == "HT-SEED-001");
        var pendingDisposal = await context.BloodDisposalRecords
            .FirstOrDefaultAsync(d => d.Status == DisposalStatus.PENDING_APPROVAL);

        var now = DateTime.UtcNow;
        var notifications = new List<Notification>();

        // 1. Thông báo cho BS Lâm sàng (doctor): Phiên bàn giao đang chờ quét nhận (Chưa đọc - Khẩn)
        if (pendingHandover != null)
        {
            notifications.Add(new Notification
            {
                RecipientUserId = doctor.UserId,
                Title = $"Mời quét xác nhận nhận máu tại quầy ({pendingHandover.SessionCode})",
                Content = $"Ngân hàng máu đã mở phiên bàn giao {pendingHandover.SessionCode}. Vui lòng quét xác nhận tiếp nhận túi máu tại quầy.",
                NotificationType = "HANDOVER_SESSION_CREATED",
                SourceModule = "Clinical",
                DeduplicationKey = $"SEED:HANDOVER_SESSION:{pendingHandover.HandoverSessionId}",
                ResourceType = "HANDOVER_SESSION",
                ResourceId = pendingHandover.HandoverSessionId,
                ActionPath = $"/clinical/handovers/{pendingHandover.HandoverSessionId}",
                Priority = NotificationPriority.Critical,
                IsRead = false,
                CreatedAt = now.AddMinutes(-8)
            });
        }

        // 2. Thông báo cho BS Lâm sàng (doctor): Phiếu yêu cầu đã sẵn sàng cấp phát (Đã đọc - Thường)
        if (readyRequest != null)
        {
            notifications.Add(new Notification
            {
                RecipientUserId = doctor.UserId,
                Title = $"Phiếu {readyRequest.RequestCode} đã được giữ chỗ đủ túi máu",
                Content = $"Ngân hàng máu đã chuẩn bị xong chế phẩm cho bệnh nhân {readyRequest.PatientName}. Khoa có thể cử nhân sự xuống nhận.",
                NotificationType = "BLOOD_REQUEST_READY",
                SourceModule = "Clinical",
                DeduplicationKey = $"SEED:REQUEST_READY:{readyRequest.RequestId}",
                ResourceType = "BLOOD_REQUEST",
                ResourceId = readyRequest.RequestId,
                ActionPath = "/clinical/requests",
                Priority = NotificationPriority.Normal,
                IsRead = true,
                ReadAt = now.AddMinutes(-20),
                CreatedAt = now.AddMinutes(-35)
            });
        }

        // 3. Thông báo cho KTV Ngân hàng máu (bloodbank): Yêu cầu máu tối khẩn mới (Chưa đọc - Khẩn)
        if (emergencyRequest != null)
        {
            notifications.Add(new Notification
            {
                RecipientUserId = bloodbank.UserId,
                Title = $"Yêu cầu máu tối khẩn mới ({emergencyRequest.RequestCode})",
                Content = $"Khoa Cấp cứu gửi yêu cầu máu tối khẩn cho bệnh nhân {emergencyRequest.PatientName}. Cần ưu tiên giữ chỗ ngay.",
                NotificationType = "BLOOD_REQUEST_SUBMITTED",
                SourceModule = "Clinical",
                DeduplicationKey = $"SEED:REQUEST_SUBMITTED:{emergencyRequest.RequestId}",
                ResourceType = "BLOOD_REQUEST",
                ResourceId = emergencyRequest.RequestId,
                ActionPath = "/coordination/queue",
                Priority = NotificationPriority.Critical,
                IsRead = false,
                CreatedAt = now.AddMinutes(-15)
            });
        }

        // 4. Thông báo cho KTV Ngân hàng máu (bloodbank): Phiếu hoàn trả chờ kiểm định (Chưa đọc - Thường)
        if (pendingReturn != null)
        {
            var barcode = pendingReturn.Allocation?.Bag?.Barcode;
            notifications.Add(new Notification
            {
                RecipientUserId = bloodbank.UserId,
                Title = $"Phiếu hoàn trả {pendingReturn.ReturnCode} chờ kiểm định",
                Content = "Khoa lâm sàng vừa bàn giao túi máu hoàn trả. Vui lòng kiểm tra ngoại quan và quyết định hướng xử lý.",
                NotificationType = "BLOOD_RETURN_CREATED",
                SourceModule = "Clinical",
                DeduplicationKey = $"SEED:RETURN_CREATED:{pendingReturn.ReturnId}",
                ResourceType = "BLOOD_RETURN",
                ResourceId = pendingReturn.ReturnId,
                ActionPath = !string.IsNullOrWhiteSpace(barcode)
                    ? $"/inventory/returns/{Uri.EscapeDataString(barcode)}"
                    : "/inventory/returns",
                Priority = NotificationPriority.Normal,
                IsRead = false,
                CreatedAt = now.AddMinutes(-25)
            });
        }

        // 5. Thông báo cho Ban Giám đốc (manager): Biên bản tiêu hủy chờ phê duyệt (Chưa đọc - Thường)
        if (pendingDisposal != null)
        {
            notifications.Add(new Notification
            {
                RecipientUserId = manager.UserId,
                Title = $"Đề xuất tiêu hủy {pendingDisposal.DisposalCode} chờ phê duyệt",
                Content = "Ngân hàng máu vừa trình biên bản đề xuất tiêu hủy túi máu không đạt chuẩn. Kính mời Ban Giám đốc xem xét.",
                NotificationType = "BLOOD_DISPOSAL_PROPOSED",
                SourceModule = "Inventory",
                DeduplicationKey = $"SEED:DISPOSAL_PROPOSED:{pendingDisposal.DisposalId}",
                ResourceType = "BLOOD_DISPOSAL",
                ResourceId = pendingDisposal.DisposalId,
                ActionPath = "/inventory/disposal-approvals",
                Priority = NotificationPriority.Normal,
                IsRead = false,
                CreatedAt = now.AddMinutes(-30)
            });
        }

        if (notifications.Count > 0)
        {
            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();
        }
    }
}
