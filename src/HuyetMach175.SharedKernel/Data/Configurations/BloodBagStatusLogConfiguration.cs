using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodBagStatusLogConfiguration : IEntityTypeConfiguration<BloodBagStatusLog>
{
    public void Configure(EntityTypeBuilder<BloodBagStatusLog> builder)
    {
        builder.ToTable("blood_bag_status_logs");

        builder.HasKey(l => l.LogId);

        builder.Property(l => l.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(l => l.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(l => l.Notes)
            .HasMaxLength(255);

        builder.HasIndex(l => l.BagId)
            .HasDatabaseName("idx_blood_bag_status_logs_bag_id");

        builder.HasIndex(l => l.CreatedAt)
            .HasDatabaseName("idx_blood_bag_status_logs_created_at");

        builder.HasOne(l => l.BloodBag)
            .WithMany(b => b.StatusLogs)
            .HasForeignKey(l => l.BagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.PerformedByUser)
            .WithMany()
            .HasForeignKey(l => l.PerformedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.AuditLog)
            .WithMany()
            .HasForeignKey(l => l.AuditLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}