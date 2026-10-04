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

    [Column("doctor_id")]
    public int DoctorId { get; set; }

    [Column("phlebotomist_id")]
    public int? PhlebotomistId { get; set; }

    [Column("weight_kg")]
    public decimal WeightKg { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("blood_pressure")]
    public string BloodPressure { get; set; } = string.Empty;

    [Column("hemoglobin_level")]
    public decimal? HemoglobinLevel { get; set; }

    [Column("screening_status")]
    public ScreeningStatus ScreeningStatus { get; set; }

    [MaxLength(255)]
    [Column("deferral_reason")]
    public string? DeferralReason { get; set; }

    [Column("target_volume_ml")]
    public TargetVolume? TargetVolumeMl { get; set; }

    [Column("actual_volume_ml")]
    public int? ActualVolumeMl { get; set; }

    [MaxLength(255)]
    [Column("collection_incident")]
    public string? CollectionIncident { get; set; }

    [Column("status")]
    public SessionStatus Status { get; set; } = SessionStatus.PENDING;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AppointmentId))]
    public DonationAppointment? Appointment { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public User? Doctor { get; set; }

    [ForeignKey(nameof(PhlebotomistId))]
    public User? Phlebotomist { get; set; }

    public ICollection<BloodBag> BloodBags { get; set; } = new List<BloodBag>();
}
