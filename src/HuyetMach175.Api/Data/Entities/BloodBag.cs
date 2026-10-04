using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("blood_bags")]
public class BloodBag
{
    [Key]
    [Column("bag_id")]
    public int BagId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [Column("parent_bag_id")]
    public int? ParentBagId { get; set; }

    [Column("session_id")]
    public int? SessionId { get; set; }

    [Column("component_type_id")]
    public int ComponentTypeId { get; set; }

    [Column("location_id")]
    public int? LocationId { get; set; }

    [Column("blood_type")]
    public BloodType BloodType { get; set; }

    [Column("rh_factor")]
    public RhFactor RhFactor { get; set; }

    [Column("volume_ml")]
    public int VolumeMl { get; set; }

    [Column("collected_at")]
    public DateTime CollectedAt { get; set; }

    [Column("expired_at")]
    public DateTime ExpiredAt { get; set; }

    [Column("status")]
    public BagStatus Status { get; set; } = BagStatus.NEWLY_COLLECTED;

    [MaxLength(255)]
    [Column("discard_reason")]
    public string? DiscardReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ParentBagId))]
    public BloodBag? ParentBag { get; set; }

    [ForeignKey(nameof(SessionId))]
    public DonationSession? Session { get; set; }

    [ForeignKey(nameof(ComponentTypeId))]
    public BloodComponentType? ComponentType { get; set; }

    [ForeignKey(nameof(LocationId))]
    public StorageLocation? Location { get; set; }

    public ICollection<BloodBag> DerivedBags { get; set; } = new List<BloodBag>();
    public ICollection<BloodTestResult> TestResults { get; set; } = new List<BloodTestResult>();
    public ICollection<BloodAllocation> Allocations { get; set; } = new List<BloodAllocation>();
}
