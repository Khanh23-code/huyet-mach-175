using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("donation_appointments")]
public class DonationAppointment
{
    [Key]
    [Column("appointment_id")]
    public int AppointmentId { get; set; }

    [Column("donor_id")]
    public int DonorId { get; set; }

    [Column("campaign_id")]
    public int? CampaignId { get; set; }

    [Column("appointment_date")]
    public DateOnly AppointmentDate { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("time_slot")]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("qr_code_token")]
    public string QrCodeToken { get; set; } = string.Empty;

    [Column("status")]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.BOOKED;

    [Column("is_walk_in")]
    public bool IsWalkIn { get; set; } = false;

    [Column("checked_in_at")]
    public DateTime? CheckedInAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(DonorId))]
    public Donor? Donor { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public DonationCampaign? Campaign { get; set; }

    public PreScreeningSurvey? PreScreeningSurvey { get; set; }
    public DonationSession? DonationSession { get; set; }
}
