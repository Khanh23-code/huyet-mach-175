using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AppointmentId))]
    public DonationAppointment? Appointment { get; set; }
}
