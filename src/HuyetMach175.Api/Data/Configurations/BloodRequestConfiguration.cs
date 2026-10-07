using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodRequestConfiguration : IEntityTypeConfiguration<BloodRequest>
{
    public void Configure(EntityTypeBuilder<BloodRequest> builder)
    {
        builder.HasIndex(br => br.RequestCode).IsUnique();

        builder.Property(br => br.PatientBloodType).HasConversion<string>();
        builder.Property(br => br.PatientRh).HasConversion<string>();
        builder.Property(br => br.UrgencyLevel).HasConversion<string>();
        builder.Property(br => br.Status).HasConversion<string>();

        builder.HasOne(br => br.Doctor)
            .WithMany(u => u.DoctorRequests)
            .HasForeignKey(br => br.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
