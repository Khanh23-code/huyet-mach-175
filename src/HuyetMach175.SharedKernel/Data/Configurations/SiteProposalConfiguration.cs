using HuyetMach175.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HuyetMach175.Api.Data.Configurations;

public class SiteProposalConfiguration : IEntityTypeConfiguration<SiteProposal>
{
    public void Configure(EntityTypeBuilder<SiteProposal> builder)
    {
        builder.Property(p => p.ProposalType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.SiteType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.ProposalCode).IsUnique();
        builder.HasIndex(p => new { p.Status, p.CreatedAt });
        builder.HasIndex(p => new { p.SiteId, p.Status });

        builder.HasOne(p => p.Site)
            .WithMany()
            .HasForeignKey(p => p.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ReviewedByUser)
            .WithMany()
            .HasForeignKey(p => p.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
