using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Type).HasConversion<string>();
        builder.Property(n => n.ReferenceType).HasConversion<string>();

        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead })
            .HasDatabaseName("idx_notifications_recipient_unread")
            .HasFilter("is_read = FALSE");

        builder.HasIndex(n => n.TargetRoleId)
            .HasDatabaseName("idx_notifications_target_role")
            .HasFilter("target_role_id IS NOT NULL");
    }
}
