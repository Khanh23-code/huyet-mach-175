using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_handover_session_items")]
public class BloodHandoverSessionItem
{
    [Key]
    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("handover_session_id")]
    public int HandoverSessionId { get; set; }

    [Column("allocation_id")]
    public int AllocationId { get; set; }

    [Column("bag_id")]
    public int BagId { get; set; }

    [ForeignKey(nameof(HandoverSessionId))]
    public BloodHandoverSession? HandoverSession { get; set; }

    [ForeignKey(nameof(AllocationId))]
    public BloodAllocation? Allocation { get; set; }

    [ForeignKey(nameof(BagId))]
    public BloodBag? Bag { get; set; }
}
