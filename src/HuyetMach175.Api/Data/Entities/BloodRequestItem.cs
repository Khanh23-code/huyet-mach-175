using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_request_items")]
public class BloodRequestItem
{
    [Key]
    [Column("request_item_id")]
    public int RequestItemId { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("component_type_id")]
    public int ComponentTypeId { get; set; }

    [Column("requested_units")]
    public int RequestedUnits { get; set; }

    [Column("allocated_units")]
    public int AllocatedUnits { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public BloodRequest? Request { get; set; }

    [ForeignKey(nameof(ComponentTypeId))]
    public BloodComponentType? ComponentType { get; set; }
}
