using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Configurations;

public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.Property(d => d.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(NotificationChannel.Email);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(NotificationDeliveryStatus.Pending);

        builder.HasOne(d => d.Notification)
            .WithMany(n => n.Deliveries)
            .HasForeignKey(d => d.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Donor)
            .WithMany(donor => donor.NotificationDeliveries)
            .HasForeignKey(d => d.DonorId)
            .OnDelete(DeleteBehavior.SetNull);

        // 1. Chống xếp trùng email cho cùng 1 thông báo In-App của nhân viên
        builder.HasIndex(d => new { d.NotificationId, d.Channel, d.Destination })
            .IsUnique()
            .HasDatabaseName("uq_notification_delivery_notif_dest")
            .HasFilter("notification_id IS NOT NULL");

        // 2. Chống xếp trùng email độc lập (ví dụ gửi cho Người hiến máu Donor khi notification_id IS NULL)
        builder.HasIndex(d => new { d.Channel, d.Destination, d.DeduplicationKey })
            .IsUnique()
            .HasDatabaseName("uq_notification_delivery_dedup")
            .HasFilter("deduplication_key IS NOT NULL");

        // 3. Partial Index giúp NotificationDeliveryWorker quét nhanh các dòng đang chờ xử lý / hết hạn lease
        builder.HasIndex(d => new { d.NextAttemptAt, d.DeliveryId })
            .HasDatabaseName("idx_notification_delivery_queue")
            .HasFilter("status IN ('Pending', 'RetryScheduled', 'Processing')");

        // 4. Tra cứu lịch sử gửi email theo Người hiến máu
        builder.HasIndex(d => d.DonorId)
            .HasDatabaseName("idx_notification_delivery_donor")
            .HasFilter("donor_id IS NOT NULL");
    }
}
