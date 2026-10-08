using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class DonorConfiguration : IEntityTypeConfiguration<Donor>
{
    public void Configure(EntityTypeBuilder<Donor> builder)
    {
        builder.HasIndex(d => d.CitizenId).IsUnique();
        builder.HasIndex(d => d.PhoneNumber).IsUnique();

        builder.Property(d => d.BloodType)
            .HasConversion<string>();

        builder.Property(d => d.RhFactor)
            .HasConversion<string>();
    }
}
