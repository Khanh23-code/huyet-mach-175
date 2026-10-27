using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Api.Data.Entities;

[Table("pre_screening_surveys")]
public class PreScreeningSurvey
{
    [Key]
    [Column("survey_id")]
    public int SurveyId { get; set; }

    [Column("appointment_id")]
    public int AppointmentId { get; set; }

    [Required]
    [Column("survey_answers", TypeName = "jsonb")]
    public string SurveyAnswers { get; set; } = "{}";

    [Column("risk_score")]
    public int RiskScore { get; set; } = 0;

    [Column("has_risk")]
    public bool HasRisk { get; set; } = false;

    [Column("survey_submitted_at")]
    public DateTime? SurveySubmittedAt { get; set; }

    [Column("doctor_id")]
    public int? DoctorId { get; set; }

    [Column("weight_kg", TypeName = "numeric(5,2)")]
    public decimal? WeightKg { get; set; }

    [Column("systolic_blood_pressure")]
    public int? SystolicBloodPressure { get; set; }

    [Column("diastolic_blood_pressure")]
    public int? DiastolicBloodPressure { get; set; }

    [Column("pulse")]
    public int? Pulse { get; set; }

    [Column("hemoglobin_level", TypeName = "numeric(5,2)")]
    public decimal? HemoglobinLevel { get; set; }

    [Column("body_temp", TypeName = "numeric(4,1)")]
    public decimal? BodyTemp { get; set; }

    [Column("rapid_hbsag_negative")]
    public bool? RapidHbsAgNegative { get; set; }

    [MaxLength(500)]
    [Column("clinical_notes")]
    public string? ClinicalNotes { get; set; }

    [Column("screening_status")]
    public ScreeningStatus ScreeningStatus { get; set; } = ScreeningStatus.PENDING;

    [MaxLength(500)]
    [Column("deferral_reason")]
    public string? DeferralReason { get; set; }

    [Column("target_volume_ml")]
    public TargetVolume? TargetVolumeMl { get; set; }

    [Column("screened_at")]
    public DateTime? ScreenedAt { get; set; }

    [ForeignKey(nameof(AppointmentId))]
    public DonationAppointment? Appointment { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public User? Doctor { get; set; }
}

