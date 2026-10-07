using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(r => r.RoleCode).IsUnique();

        builder.Property(r => r.RoleCode)
            .HasConversion<string>();
    }
}
