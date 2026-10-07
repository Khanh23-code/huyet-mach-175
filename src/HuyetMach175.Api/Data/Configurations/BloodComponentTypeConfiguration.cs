using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodComponentTypeConfiguration : IEntityTypeConfiguration<BloodComponentType>
{
    public void Configure(EntityTypeBuilder<BloodComponentType> builder)
    {
        builder.HasIndex(bct => bct.TypeCode).IsUnique();

        builder.Property(bct => bct.TypeCode)
            .HasConversion<string>();
    }
}
