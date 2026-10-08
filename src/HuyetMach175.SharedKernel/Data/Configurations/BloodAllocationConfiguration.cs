using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodAllocationConfiguration : IEntityTypeConfiguration<BloodAllocation>
{
    public void Configure(EntityTypeBuilder<BloodAllocation> builder)
    {
        builder.Property(ba => ba.Status).HasConversion<string>();

        builder.HasIndex(ba => ba.BagId)
            .HasDatabaseName("uq_active_allocation_per_bag")
            .HasFilter("status IN ('RESERVED', 'ISSUED')")
            .IsUnique();

        builder.HasOne(ba => ba.ReservedByUser)
            .WithMany(u => u.ReservedAllocations)
            .HasForeignKey(ba => ba.ReservedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ba => ba.IssuedByUser)
            .WithMany(u => u.IssuedAllocations)
            .HasForeignKey(ba => ba.IssuedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ba => ba.ReceivedByUser)
            .WithMany(u => u.ReceivedAllocations)
            .HasForeignKey(ba => ba.ReceivedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
