using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodReturnConfiguration : IEntityTypeConfiguration<BloodReturn>
{
    public void Configure(EntityTypeBuilder<BloodReturn> builder)
    {
        builder.Property(br => br.FinalAction).HasConversion<string>();

        builder.HasOne(br => br.ReturnedByUser)
            .WithMany(u => u.ReturnedReturns)
            .HasForeignKey(br => br.ReturnedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(br => br.ReceivedByUser)
            .WithMany(u => u.ReceivedReturns)
            .HasForeignKey(br => br.ReceivedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
