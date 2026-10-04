using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_component_types")]
public class BloodComponentType
{
    [Key]
    [Column("component_type_id")]
    public int ComponentTypeId { get; set; }

    [Column("type_code")]
    public ComponentCode TypeCode { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("type_name")]
    public string TypeName { get; set; } = string.Empty;

    [Column("life_days")]
    public int LifeDays { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("storage_temperature")]
    public string StorageTemperature { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
    public ICollection<BloodRequestItem> RequestItems { get; set; } = new List<BloodRequestItem>();
}
