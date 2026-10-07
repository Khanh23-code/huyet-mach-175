using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class DonationSessionConfiguration : IEntityTypeConfiguration<DonationSession>
{
    public void Configure(EntityTypeBuilder<DonationSession> builder)
    {
        builder.Property(s => s.ScreeningStatus)
            .HasConversion<string>();

        builder.Property(s => s.TargetVolumeMl)
            .HasConversion<string>();

        builder.Property(s => s.Status)
            .HasConversion<string>();

        builder.HasOne(s => s.Doctor)
            .WithMany(u => u.DoctorSessions)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Phlebotomist)
            .WithMany(u => u.PhlebotomistSessions)
            .HasForeignKey(s => s.PhlebotomistId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
