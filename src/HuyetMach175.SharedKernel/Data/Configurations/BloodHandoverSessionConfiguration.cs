using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodHandoverSessionConfiguration : IEntityTypeConfiguration<BloodHandoverSession>
{
    public void Configure(EntityTypeBuilder<BloodHandoverSession> builder)
    {
        builder.HasIndex(hs => hs.SessionCode).IsUnique();

        builder.Property(hs => hs.Status).HasConversion<string>();

        builder.HasIndex(hs => new { hs.Status, hs.ExpiresAt })
            .HasDatabaseName("idx_blood_handover_sessions_status_expires_at");

        builder.HasOne(hs => hs.Request)
            .WithMany(r => r.HandoverSessions)
            .HasForeignKey(hs => hs.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(hs => hs.InitiatedByUser)
            .WithMany()
            .HasForeignKey(hs => hs.InitiatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(hs => hs.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(hs => hs.ConfirmedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(hs => hs.CancelledByUser)
            .WithMany()
            .HasForeignKey(hs => hs.CancelledBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
