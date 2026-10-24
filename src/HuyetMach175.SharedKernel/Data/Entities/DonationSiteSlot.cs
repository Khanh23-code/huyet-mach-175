using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

/// <summary>Khung giờ + sức chứa mỗi slot của một điểm hiến.</summary>
[Table("donation_site_slots")]
public class DonationSiteSlot
{
    [Key]
    [Column("slot_id")]
    public int SlotId { get; set; }

    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly EndTime { get; set; }

    [Column("capacity")]
    public int Capacity { get; set; }

    [ForeignKey(nameof(SiteId))]
    public DonationSite? Site { get; set; }
}
