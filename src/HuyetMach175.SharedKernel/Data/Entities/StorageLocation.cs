using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("storage_locations")]
public class StorageLocation
{
    [Key]
    [Column("location_id")]
    public int LocationId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("refrigerator_name")]
    public string RefrigeratorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("shelf_number")]
    public string ShelfNumber { get; set; } = string.Empty;

    [Column("target_temperature")]
    public decimal TargetTemperature { get; set; }

    [Column("is_full")]
    public bool IsFull { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
}
