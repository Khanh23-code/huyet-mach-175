using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodDisposalItemConfiguration : IEntityTypeConfiguration<BloodDisposalItem>
{
    public void Configure(EntityTypeBuilder<BloodDisposalItem> builder)
    {
        builder.Property(i => i.PreviousStatus).HasConversion<string>();

        builder.HasOne(i => i.DisposalRecord)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.DisposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.BloodBag)
            .WithMany(b => b.DisposalItems)
            .HasForeignKey(i => i.BagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

