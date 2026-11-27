using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_disposal_items")]
public class BloodDisposalItem
{
    [Key]
    [Column("item_id")]
    public Guid ItemId { get; set; } = Guid.CreateVersion7();

    [Column("disposal_id")]
    public Guid DisposalId { get; set; }

    [Column("bag_id")]
    public Guid BagId { get; set; }

    [Column("previous_status")]
    public BagStatus PreviousStatus { get; set; }

    [Column("is_verified")]
    public bool IsVerified { get; set; } = true;

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [ForeignKey(nameof(DisposalId))]
    public BloodDisposalRecord? DisposalRecord { get; set; }

    [ForeignKey(nameof(BagId))]
    public BloodBag? BloodBag { get; set; }
}

