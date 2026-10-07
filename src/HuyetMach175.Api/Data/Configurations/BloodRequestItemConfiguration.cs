using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodRequestItemConfiguration : IEntityTypeConfiguration<BloodRequestItem>
{
    public void Configure(EntityTypeBuilder<BloodRequestItem> builder)
    {
        builder.HasOne(item => item.Request)
            .WithMany(req => req.Items)
            .HasForeignKey(item => item.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.ComponentType)
            .WithMany(bct => bct.RequestItems)
            .HasForeignKey(item => item.ComponentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
