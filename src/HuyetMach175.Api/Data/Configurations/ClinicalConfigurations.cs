using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodRequestConfiguration : IEntityTypeConfiguration<BloodRequest>
{
    public void Configure(EntityTypeBuilder<BloodRequest> builder)
    {
        builder.HasIndex(br => br.RequestCode).IsUnique();

        builder.HasIndex(br => new { br.DepartmentId, br.Status })
            .HasDatabaseName("idx_blood_requests_dept_status");

        builder.HasOne(br => br.Doctor)
            .WithMany(u => u.DoctorRequests)
            .HasForeignKey(br => br.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BloodAllocationConfiguration : IEntityTypeConfiguration<BloodAllocation>
{
    public void Configure(EntityTypeBuilder<BloodAllocation> builder)
    {
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

public class BloodReturnConfiguration : IEntityTypeConfiguration<BloodReturn>
{
    public void Configure(EntityTypeBuilder<BloodReturn> builder)
    {
        builder.HasOne(br => br.ReturnedByUser)
            .WithMany(u => u.ReturnedReturns)
            .HasForeignKey(br => br.ReturnedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(br => br.ReceivedByUser)
            .WithMany(u => u.ReceivedReturns)
            .HasForeignKey(br => br.ReceivedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
