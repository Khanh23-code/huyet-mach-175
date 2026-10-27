using System.Text.Json;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class SurveyDetailDto
{
    public int AppointmentId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public int? SlotId { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string QrCodeToken { get; set; } = string.Empty;
    public AppointmentStatus AppointmentStatus { get; set; }
    public bool IsWalkIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public int? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public string? CampaignLocation { get; set; }

    public SurveyDonorSummaryDto Donor { get; set; } = new();

    public int? SurveyId { get; set; }

    public bool HasSubmittedSurvey { get; set; }
    public JsonElement? SurveyAnswers { get; set; }
    public int RiskScore { get; set; }
    public bool HasRisk { get; set; }
    public DateTime? SurveySubmittedAt { get; set; }

    public decimal? WeightKg { get; set; }
    public int? SystolicBloodPressure { get; set; }
    public int? DiastolicBloodPressure { get; set; }
    public int? Pulse { get; set; }
    public decimal? HemoglobinLevel { get; set; }
    public decimal? BodyTemp { get; set; }
    public bool? RapidHbsAgNegative { get; set; }
    public string? ClinicalNotes { get; set; }

    public int? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public ScreeningStatus ScreeningStatus { get; set; } = ScreeningStatus.PENDING;
    public string? DeferralReason { get; set; }
    public TargetVolume? TargetVolumeMl { get; set; }
    public DateTime? ScreenedAt { get; set; }

    public int? SessionId { get; set; }
    public SessionStatus? SessionStatus { get; set; }
}