using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(al => al.ActionType).HasConversion<string>();

        builder.HasIndex(al => new { al.TableName, al.RecordId, al.CreatedAt })
            .HasDatabaseName("idx_audit_logs_record");

        builder.HasOne(al => al.PerformedByUser)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.PerformedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
