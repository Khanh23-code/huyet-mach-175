using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodDisposalRecordConfiguration : IEntityTypeConfiguration<BloodDisposalRecord>
{
    public void Configure(EntityTypeBuilder<BloodDisposalRecord> builder)
    {
        builder.HasIndex(b => b.DisposalCode).IsUnique();

        builder.Property(b => b.Reason).HasConversion<string>();
        builder.Property(b => b.Status).HasConversion<string>();
        builder.Property(b => b.PreviousStatus).HasConversion<string>();

        builder.HasOne(b => b.BloodBag)
            .WithMany(bb => bb.DisposalRecords)
            .HasForeignKey(b => b.BagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.RequestedByUser)
            .WithMany()
            .HasForeignKey(b => b.RequestedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ApprovedByUser)
            .WithMany()
            .HasForeignKey(b => b.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

