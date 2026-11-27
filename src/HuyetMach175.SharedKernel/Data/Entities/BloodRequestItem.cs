using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_request_items")]
public class BloodRequestItem
{
    [Key]
    [Column("request_item_id")]
    public Guid RequestItemId { get; set; } = Guid.CreateVersion7();

    [Column("request_id")]
    public Guid RequestId { get; set; }

    [Column("component_type_id")]
    public Guid ComponentTypeId { get; set; }

    [Column("requested_units")]
    public int RequestedUnits { get; set; }

    [Column("allocated_units")]
    public int AllocatedUnits { get; set; } = 0;

    [ForeignKey(nameof(RequestId))]
    public BloodRequest? Request { get; set; }

    [ForeignKey(nameof(ComponentTypeId))]
    public BloodComponentType? ComponentType { get; set; }
}
