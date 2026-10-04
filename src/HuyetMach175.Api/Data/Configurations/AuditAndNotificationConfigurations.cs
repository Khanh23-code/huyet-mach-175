using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(al => new { al.TableName, al.RecordId, al.CreatedAt })
            .HasDatabaseName("idx_audit_logs_record");

        builder.HasOne(al => al.PerformedByUser)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.PerformedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead })
            .HasDatabaseName("idx_notifications_recipient_unread")
            .HasFilter("is_read = FALSE");

        builder.HasIndex(n => n.TargetRoleId)
            .HasDatabaseName("idx_notifications_target_role")
            .HasFilter("target_role_id IS NOT NULL");
    }
}
