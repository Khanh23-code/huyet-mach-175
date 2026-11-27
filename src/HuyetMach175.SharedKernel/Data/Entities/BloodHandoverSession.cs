using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_handover_sessions")]
public class BloodHandoverSession
{
    [Key]
    [Column("handover_session_id")]
    public Guid HandoverSessionId { get; set; } = Guid.CreateVersion7();

    [Required]
    [MaxLength(30)]
    [Column("session_code")]
    public string SessionCode { get; set; } = string.Empty;

    [Column("request_id")]
    public Guid RequestId { get; set; }

    [Column("initiated_by")]
    public Guid InitiatedBy { get; set; }

    [Column("confirmed_by")]
    public Guid? ConfirmedBy { get; set; }

    [Column("cancelled_by")]
    public Guid? CancelledBy { get; set; }

    [Column("status")]
    public HandoverSessionStatus Status { get; set; } = HandoverSessionStatus.PENDING;

    [MaxLength(255)]
    [Column("cancel_reason")]
    public string? CancelReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [ForeignKey(nameof(RequestId))]
    public BloodRequest? Request { get; set; }

    [ForeignKey(nameof(InitiatedBy))]
    public User? InitiatedByUser { get; set; }

    [ForeignKey(nameof(ConfirmedBy))]
    public User? ConfirmedByUser { get; set; }

    [ForeignKey(nameof(CancelledBy))]
    public User? CancelledByUser { get; set; }

    public ICollection<BloodHandoverSessionItem> Items { get; set; } = new List<BloodHandoverSessionItem>();
}
