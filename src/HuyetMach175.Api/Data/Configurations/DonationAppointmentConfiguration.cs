using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class DonationAppointmentConfiguration : IEntityTypeConfiguration<DonationAppointment>
{
    public void Configure(EntityTypeBuilder<DonationAppointment> builder)
    {
        builder.HasIndex(da => da.QrCodeToken).IsUnique();

        builder.Property(da => da.Status)
            .HasConversion<string>();

        builder.HasIndex(da => new { da.DonorId, da.AppointmentDate })
            .HasDatabaseName("uq_active_appointment_per_day")
            .HasFilter("status = 'BOOKED'")
            .IsUnique();
    }
}
