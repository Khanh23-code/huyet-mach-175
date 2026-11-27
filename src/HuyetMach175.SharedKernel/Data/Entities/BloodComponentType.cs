using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_component_types")]
public class BloodComponentType
{
    [Key]
    [Column("component_type_id")]
    public Guid ComponentTypeId { get; set; } = Guid.CreateVersion7();

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

    [Column("min_temp", TypeName = "numeric(5,2)")]
    public decimal MinTemp { get; set; }

    [Column("max_temp", TypeName = "numeric(5,2)")]
    public decimal MaxTemp { get; set; }

    [Column("allow_restock")]
    public bool AllowRestock { get; set; }

    [Column("max_outside_minutes_for_restock")]
    public int? MaxOutsideMinutesForRestock { get; set; }

    [Column("max_restock_count")]
    public int MaxRestockCount { get; set; } = 1;

    [Column("compatibility_rule_type")]
    public CompatibilityRuleType CompatibilityRuleType { get; set; } = CompatibilityRuleType.RBC;

    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
    public ICollection<BloodRequestItem> RequestItems { get; set; } = new List<BloodRequestItem>();
}
