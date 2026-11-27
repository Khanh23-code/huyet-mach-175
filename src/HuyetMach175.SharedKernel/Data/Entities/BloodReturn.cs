using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_returns")]
public class BloodReturn
{
    [Key]
    [Column("return_id")]
    public Guid ReturnId { get; set; } = Guid.CreateVersion7();

    [Required]
    [MaxLength(30)]
    [Column("return_code")]
    public string ReturnCode { get; set; } = string.Empty;

    [Column("allocation_id")]
    public Guid AllocationId { get; set; }

    [Column("returned_by")]
    public Guid ReturnedBy { get; set; }

    [Column("received_by")]
    public Guid ReceivedBy { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("return_reason")]
    public string ReturnReason { get; set; } = string.Empty;

    [Column("qualified")]
    public bool Qualified { get; set; }

    [Column("final_action")]
    public ReturnFinalAction FinalAction { get; set; }

    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AllocationId))]
    public BloodAllocation? Allocation { get; set; }

    [ForeignKey(nameof(ReturnedBy))]
    public User? ReturnedByUser { get; set; }

    [ForeignKey(nameof(ReceivedBy))]
    public User? ReceivedByUser { get; set; }
}
