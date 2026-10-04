using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class DonorConfiguration : IEntityTypeConfiguration<Donor>
{
    public void Configure(EntityTypeBuilder<Donor> builder)
    {
        builder.HasIndex(d => d.CitizenId).IsUnique();
        builder.HasIndex(d => d.PhoneNumber).IsUnique();
    }
}

public class DonationAppointmentConfiguration : IEntityTypeConfiguration<DonationAppointment>
{
    public void Configure(EntityTypeBuilder<DonationAppointment> builder)
    {
        builder.HasIndex(da => da.QrCodeToken).IsUnique();

        builder.HasIndex(da => new { da.DonorId, da.AppointmentDate })
            .HasDatabaseName("uq_active_appointment_per_day")
            .HasFilter("status = 'BOOKED'")
            .IsUnique();
    }
}

public class DonationCampaignConfiguration : IEntityTypeConfiguration<DonationCampaign>
{
    public void Configure(EntityTypeBuilder<DonationCampaign> builder)
    {
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

public class DonationSessionConfiguration : IEntityTypeConfiguration<DonationSession>
{
    public void Configure(EntityTypeBuilder<DonationSession> builder)
    {
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
