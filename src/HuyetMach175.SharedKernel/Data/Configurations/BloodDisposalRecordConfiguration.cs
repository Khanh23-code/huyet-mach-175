using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Api.Data.Configurations;

public class BloodDisposalRecordConfiguration : IEntityTypeConfiguration<BloodDisposalRecord>
{
    public void Configure(EntityTypeBuilder<BloodDisposalRecord> builder)
    {
        builder.HasIndex(b => b.DisposalCode).IsUnique();

        builder.Property(b => b.Reason).HasConversion<string>();
        builder.Property(b => b.Status).HasConversion<string>();
<<<<<<< HEAD
        builder.Property(b => b.PreviousStatus).HasConversion<string>();

        builder.HasOne(b => b.BloodBag)
            .WithMany(bb => bb.DisposalRecords)
            .HasForeignKey(b => b.BagId)
            .OnDelete(DeleteBehavior.Restrict);
=======
>>>>>>> 18fd47d92a9274d24c20e3913cd951c12967d593

        builder.HasOne(b => b.RequestedByUser)
            .WithMany()
            .HasForeignKey(b => b.RequestedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ApprovedByUser)
            .WithMany()
            .HasForeignKey(b => b.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
<<<<<<< HEAD
    }
}

=======

        builder.HasMany(b => b.Items)
            .WithOne(i => i.DisposalRecord)
            .HasForeignKey(i => i.DisposalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
>>>>>>> 18fd47d92a9274d24c20e3913cd951c12967d593
