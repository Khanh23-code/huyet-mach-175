using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.StaffCode).IsUnique();

        // PostgreSQL cho phép nhiều giá trị NULL trong unique index nên không cần HasFilter.
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.PhoneNumber).IsUnique();
    }
}