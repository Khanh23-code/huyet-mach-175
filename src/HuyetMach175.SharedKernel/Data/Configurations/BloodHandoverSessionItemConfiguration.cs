using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodHandoverSessionItemConfiguration : IEntityTypeConfiguration<BloodHandoverSessionItem>
{
    public void Configure(EntityTypeBuilder<BloodHandoverSessionItem> builder)
    {
        builder.HasIndex(i => new { i.HandoverSessionId, i.AllocationId }).IsUnique();

        builder.HasOne(i => i.HandoverSession)
            .WithMany(hs => hs.Items)
            .HasForeignKey(i => i.HandoverSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Allocation)
            .WithMany()
            .HasForeignKey(i => i.AllocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Bag)
            .WithMany()
            .HasForeignKey(i => i.BagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
