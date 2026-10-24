using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

/// <summary>Điểm hiến máu đang phát hành (cố định / lưu động).</summary>
[Table("donation_sites")]
public class DonationSite
{
    [Key]
    [Column("site_id")]
    public int SiteId { get; set; }

    [MaxLength(30)]
    [Column("site_code")]
    public string? SiteCode { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("site_name")]
    public string SiteName { get; set; } = string.Empty;

    [Column("site_type")]
    public SiteType SiteType { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DonationSiteSlot> Slots { get; set; } = new List<DonationSiteSlot>();
}
