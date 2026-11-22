using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("donation_sessions")]
public class DonationSession
{
    [Key]
    [Column("session_id")]
    public int SessionId { get; set; }

    [Column("appointment_id")]
    public int AppointmentId { get; set; }

    [Column("phlebotomist_id")]
    public int? PhlebotomistId { get; set; }

    [Column("target_volume_ml")]
    public TargetVolume TargetVolumeMl { get; set; }

    [Column("actual_volume_ml")]
    public int? ActualVolumeMl { get; set; }

    [MaxLength(500)]
    [Column("collection_incident")]
    public string? CollectionIncident { get; set; }

    [Column("status")]
    public SessionStatus Status { get; set; } = SessionStatus.WAITING_COLLECTION;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(AppointmentId))]
    public DonationAppointment? Appointment { get; set; }

    [ForeignKey(nameof(PhlebotomistId))]
    public User? Phlebotomist { get; set; }

    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
}
