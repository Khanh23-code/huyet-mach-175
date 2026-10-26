using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuyetMach175.Api.Data.Entities;

[Table("donation_campaign_slots")]
public class DonationCampaignSlot
{
    [Key]
    [Column("slot_id")]
    public int SlotId { get; set; }

    [Column("campaign_id")]
    public int CampaignId { get; set; }

    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly EndTime { get; set; }

    [Column("capacity")]
    public int Capacity { get; set; } = 20;

    [ForeignKey(nameof(CampaignId))]
    public DonationCampaign? Campaign { get; set; }

    public ICollection<DonationAppointment> Appointments { get; set; } = new List<DonationAppointment>();
}