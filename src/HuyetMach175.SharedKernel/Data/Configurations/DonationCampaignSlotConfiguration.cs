using HuyetMach175.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuyetMach175.Api.Data.Configurations;

public class DonationCampaignSlotConfiguration : IEntityTypeConfiguration<DonationCampaignSlot>
{
    public void Configure(EntityTypeBuilder<DonationCampaignSlot> builder)
    {
        builder.HasIndex(s => s.CampaignId);

        builder.HasMany(s => s.Appointments)
            .WithOne(a => a.CampaignSlot)
            .HasForeignKey(a => a.SlotId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

