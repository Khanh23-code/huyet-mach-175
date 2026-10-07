using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodTestResultConfiguration : IEntityTypeConfiguration<BloodTestResult>
{
    public void Configure(EntityTypeBuilder<BloodTestResult> builder)
    {
        builder.Property(tr => tr.HivResult).HasConversion<string>();
        builder.Property(tr => tr.HbvResult).HasConversion<string>();
        builder.Property(tr => tr.HcvResult).HasConversion<string>();
        builder.Property(tr => tr.SyphilisResult).HasConversion<string>();
        builder.Property(tr => tr.IrregularAntibody).HasConversion<string>();
        builder.Property(tr => tr.ConfirmedBloodType).HasConversion<string>();
        builder.Property(tr => tr.ConfirmedRh).HasConversion<string>();
        builder.Property(tr => tr.OverallConclusion).HasConversion<string>();

        builder.HasOne(tr => tr.Technician)
            .WithMany(u => u.TestResults)
            .HasForeignKey(tr => tr.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
