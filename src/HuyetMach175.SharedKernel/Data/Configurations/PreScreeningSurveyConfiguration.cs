using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class PreScreeningSurveyConfiguration : IEntityTypeConfiguration<PreScreeningSurvey>
{
    public void Configure(EntityTypeBuilder<PreScreeningSurvey> builder)
    {
        builder.HasIndex(s => s.AppointmentId).IsUnique();

        builder.Property(s => s.ScreeningStatus)
            .HasConversion<string>();

        builder.Property(s => s.TargetVolumeMl)
            .HasConversion<string>();

        builder.HasOne(s => s.Appointment)
            .WithOne(da => da.PreScreeningSurvey)
            .HasForeignKey<PreScreeningSurvey>(s => s.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Doctor)
            .WithMany(u => u.DoctorScreenings)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
