using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(NotificationPriority.Normal);

        builder.HasOne(n => n.RecipientUser)
            .WithMany(u => u.ReceivedNotifications)
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1. Phân trang hộp thư ổn định (recipient_user_id, created_at DESC, notification_id DESC)
        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt, n.NotificationId })
            .IsDescending(false, true, true)
            .HasDatabaseName("idx_notification_recipient_inbox");

        // 2. Đếm số chưa đọc và lọc tab "Chưa đọc" nhanh
        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_notification_recipient_unread")
            .HasFilter("is_read = FALSE");

        // 3. Chống tạo trùng thông báo theo deduplication_key trên mỗi người nhận
        builder.HasIndex(n => new { n.RecipientUserId, n.DeduplicationKey })
            .IsUnique()
            .HasDatabaseName("uq_notification_recipient_dedup")
            .HasFilter("deduplication_key IS NOT NULL");
    }
}
