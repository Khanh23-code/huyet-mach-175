using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("donation_campaigns")]
public class DonationCampaign
{
    [Key]
    [Column("campaign_id")]
    public int CampaignId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("campaign_name")]
    public string CampaignName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("location_name")]
    public string LocationName { get; set; } = string.Empty;

    [Column("campaign_type")]
    public CampaignType CampaignType { get; set; } = CampaignType.EVENT;

    [MaxLength(50)]
    [Column("operating_days")]
    public string? OperatingDays { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("target_donations")]
    public int TargetDonations { get; set; } = 100;

    [Column("max_capacity")]
    public int? MaxCapacity { get; set; }

    [Column("status")]
    public CampaignStatus Status { get; set; } = CampaignStatus.DRAFT;

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [MaxLength(255)]
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("cancelled_by")]
    public int? CancelledBy { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }

    [ForeignKey(nameof(ApprovedBy))]
    public User? ApprovedByUser { get; set; }

    [ForeignKey(nameof(CancelledBy))]
    public User? CancelledByUser { get; set; }

    public ICollection<DonationAppointment> Appointments { get; set; } = new List<DonationAppointment>();
    public ICollection<DonationCampaignSlot> Slots { get; set; } = new List<DonationCampaignSlot>();
}