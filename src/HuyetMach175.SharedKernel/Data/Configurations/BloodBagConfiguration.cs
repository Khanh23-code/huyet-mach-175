using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodBagConfiguration : IEntityTypeConfiguration<BloodBag>
{
    public void Configure(EntityTypeBuilder<BloodBag> builder)
    {
        builder.HasIndex(bb => bb.Barcode).IsUnique();

        builder.Property(bb => bb.BloodType)
            .HasConversion<string>();

        builder.Property(bb => bb.RhFactor)
            .HasConversion<string>();

        builder.Property(bb => bb.Status)
            .HasConversion<string>();

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
