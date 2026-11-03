using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_disposal_records")]
public class BloodDisposalRecord
{
    [Key]
    [Column("disposal_id")]
    public int DisposalId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("disposal_code")]
    public string DisposalCode { get; set; } = string.Empty;

    [Column("bag_id")]
    public int BagId { get; set; }

    [Column("reason")]
    public DisposalReason Reason { get; set; }

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    public DisposalStatus Status { get; set; } = DisposalStatus.PENDING_APPROVAL;

    [Column("previous_status")]
    public BagStatus PreviousStatus { get; set; }

    [Column("requested_by")]
    public int RequestedBy { get; set; }

    [Column("requested_at")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [ForeignKey(nameof(BagId))]
    public BloodBag? BloodBag { get; set; }

    [ForeignKey(nameof(RequestedBy))]
    public User? RequestedByUser { get; set; }

    [ForeignKey(nameof(ApprovedBy))]
    public User? ApprovedByUser { get; set; }
}