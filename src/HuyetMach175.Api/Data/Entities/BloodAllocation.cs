using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_allocations")]
public class BloodAllocation
{
    [Key]
    [Column("allocation_id")]
    public int AllocationId { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("bag_id")]
    public int BagId { get; set; }

    [Column("reserved_by")]
    public int ReservedBy { get; set; }

    [Column("reserved_at")]
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    [Column("issued_by")]
    public int? IssuedBy { get; set; }

    [Column("received_by")]
    public int? ReceivedBy { get; set; }

    [Column("issued_at")]
    public DateTime? IssuedAt { get; set; }

    [Column("status")]
    public AllocationStatus Status { get; set; } = AllocationStatus.RESERVED;

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public BloodRequest? Request { get; set; }

    [ForeignKey(nameof(BagId))]
    public BloodBag? Bag { get; set; }

    [ForeignKey(nameof(ReservedBy))]
    public User? ReservedByUser { get; set; }

    [ForeignKey(nameof(IssuedBy))]
    public User? IssuedByUser { get; set; }

    [ForeignKey(nameof(ReceivedBy))]
    public User? ReceivedByUser { get; set; }

    public BloodReturn? BloodReturn { get; set; }
}
