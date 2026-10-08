using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("log_id")]
    public long LogId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("table_name")]
    public string TableName { get; set; } = string.Empty;

    [Column("record_id")]
    public int RecordId { get; set; }

    [Column("action_type")]
    public ActionType ActionType { get; set; }

    [Column("old_state", TypeName = "jsonb")]
    public string? OldState { get; set; }

    [Required]
    [Column("new_state", TypeName = "jsonb")]
    public string NewState { get; set; } = "{}";

    [Column("performed_by")]
    public int? PerformedBy { get; set; }

    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [MaxLength(255)]
    [Column("change_reason")]
    public string? ChangeReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PerformedBy))]
    public User? PerformedByUser { get; set; }
}
