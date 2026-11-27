using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_disposal_records")]
public class BloodDisposalRecord
{
    [Key]
    [Column("disposal_id")]
    public Guid DisposalId { get; set; } = Guid.CreateVersion7();

    [Required]
    [MaxLength(30)]
    [Column("disposal_code")]
    public string DisposalCode { get; set; } = string.Empty;

    [Column("reason")]
    public DisposalReason Reason { get; set; }

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    public DisposalStatus Status { get; set; } = DisposalStatus.PENDING_APPROVAL;

    [Column("total_bags")]
    public int TotalBags { get; set; }

    [Column("requested_by")]
    public Guid RequestedBy { get; set; }

    [Column("requested_at")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [Column("approved_by")]
    public Guid? ApprovedBy { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [ForeignKey(nameof(RequestedBy))]
    public User? RequestedByUser { get; set; }

    [ForeignKey(nameof(ApprovedBy))]
    public User? ApprovedByUser { get; set; }

    public ICollection<BloodDisposalItem> Items { get; set; } = new List<BloodDisposalItem>();
}
