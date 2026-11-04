using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_bag_status_logs")]
public class BloodBagStatusLog
{
    [Key]
    [Column("log_id")]
    public int LogId { get; set; }

    [Column("bag_id")]
    public int BagId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("from_status")]
    public BagStatus FromStatus { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("to_status")]
    public BagStatus ToStatus { get; set; }

    [Column("performed_by_id")]
    public int? PerformedById { get; set; }

    [Column("audit_log_id")]
    public long? AuditLogId { get; set; }

    [MaxLength(255)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(BagId))]
    public BloodBag? BloodBag { get; set; }

    [ForeignKey(nameof(PerformedById))]
    public User? PerformedByUser { get; set; }

    [ForeignKey(nameof(AuditLogId))]
    public AuditLog? AuditLog { get; set; }
}