using HuyetMach175.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuyetMach175.Api.Data.Configurations;

public class DonationSiteConfiguration : IEntityTypeConfiguration<DonationSite>
{
    public void Configure(EntityTypeBuilder<DonationSite> builder)
    {
        builder.Property(s => s.SiteType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.SiteCode).IsUnique();

        builder.HasMany(s => s.Slots)
            .WithOne(x => x.Site)
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DonationSiteSlotConfiguration : IEntityTypeConfiguration<DonationSiteSlot>
{
    public void Configure(EntityTypeBuilder<DonationSiteSlot> builder)
    {
        builder.HasIndex(x => new { x.SiteId, x.StartTime });
    }
}
