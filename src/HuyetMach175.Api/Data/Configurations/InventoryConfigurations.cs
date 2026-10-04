using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodComponentTypeConfiguration : IEntityTypeConfiguration<BloodComponentType>
{
    public void Configure(EntityTypeBuilder<BloodComponentType> builder)
    {
        builder.HasIndex(bct => bct.TypeCode).IsUnique();
    }
}

public class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> builder)
    {
        builder.HasIndex(sl => sl.StorageCode).IsUnique();
    }
}

public class BloodBagConfiguration : IEntityTypeConfiguration<BloodBag>
{
    public void Configure(EntityTypeBuilder<BloodBag> builder)
    {
        builder.HasIndex(bb => bb.Barcode).IsUnique();

        builder.HasIndex(bb => new { bb.BloodType, bb.RhFactor, bb.ComponentTypeId, bb.Status, bb.ExpiredAt })
            .HasDatabaseName("idx_blood_bags_lookup");

        builder.HasOne(bb => bb.ParentBag)
            .WithMany(bb => bb.DerivedBags)
            .HasForeignKey(bb => bb.ParentBagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bb => bb.Location)
            .WithMany(l => l.BloodBags)
            .HasForeignKey(bb => bb.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class BloodTestResultConfiguration : IEntityTypeConfiguration<BloodTestResult>
{
    public void Configure(EntityTypeBuilder<BloodTestResult> builder)
    {
        builder.HasOne(tr => tr.Technician)
            .WithMany(u => u.TestResults)
            .HasForeignKey(tr => tr.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
