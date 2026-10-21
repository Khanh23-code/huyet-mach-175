using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class DonationCampaignConfiguration : IEntityTypeConfiguration<DonationCampaign>
{
    public void Configure(EntityTypeBuilder<DonationCampaign> builder)
    {
        builder.Property(c => c.Status)
            .HasConversion<string>();

        builder.HasOne(c => c.CreatedByUser)
            .WithMany(u => u.CreatedCampaigns)
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ApprovedByUser)
            .WithMany(u => u.ApprovedCampaigns)
            .HasForeignKey(c => c.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
