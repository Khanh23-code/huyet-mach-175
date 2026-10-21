using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class PreScreeningSurveyConfiguration : IEntityTypeConfiguration<PreScreeningSurvey>
{
    public void Configure(EntityTypeBuilder<PreScreeningSurvey> builder)
    {
        builder.HasIndex(s => s.AppointmentId).IsUnique();

        builder.HasOne(s => s.Appointment)
            .WithOne(da => da.PreScreeningSurvey)
            .HasForeignKey<PreScreeningSurvey>(s => s.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
