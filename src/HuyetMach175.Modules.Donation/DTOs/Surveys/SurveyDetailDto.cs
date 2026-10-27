using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class SurveyDetailDto
{
    [JsonPropertyName("appointmentId")]
    public int AppointmentId { get; set; }

    [JsonPropertyName("appointmentDate")]
    public DateOnly AppointmentDate { get; set; }

    [JsonPropertyName("timeSlot")]
    public string TimeSlot { get; set; } = string.Empty;

    [JsonPropertyName("isWalkIn")]
    public bool IsWalkIn { get; set; }

    [JsonPropertyName("checkedInAt")]
    public DateTime? CheckedInAt { get; set; }

    [JsonPropertyName("campaignName")]
    public string? CampaignName { get; set; }

    [JsonPropertyName("donor")]
    public SurveyDonorSummaryDto Donor { get; set; } = new();

    [JsonPropertyName("survey")]
    public SurveyOnlineDetailDto Survey { get; set; } = new();

    [JsonPropertyName("clinical")]
    public SurveyClinicalDetailDto Clinical { get; set; } = new();
}

public class SurveyOnlineDetailDto
{
    [JsonPropertyName("surveyId")]
    public int? SurveyId { get; set; }

    [JsonPropertyName("hasSubmittedSurvey")]
    public bool HasSubmittedSurvey { get; set; }

    [JsonPropertyName("surveySubmittedAt")]
    public DateTime? SurveySubmittedAt { get; set; }

    [JsonPropertyName("riskScore")]
    public int RiskScore { get; set; }

    [JsonPropertyName("hasRisk")]
    public bool HasRisk { get; set; }

    [JsonPropertyName("surveyAnswers")]
    public JsonElement? SurveyAnswers { get; set; }
}

public class SurveyClinicalDetailDto
{
    [JsonPropertyName("weightKg")]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("systolicBloodPressure")]
    public int? SystolicBloodPressure { get; set; }

    [JsonPropertyName("diastolicBloodPressure")]
    public int? DiastolicBloodPressure { get; set; }

    [JsonPropertyName("pulse")]
    public int? Pulse { get; set; }

    [JsonPropertyName("hemoglobinLevel")]
    public decimal? HemoglobinLevel { get; set; }

    [JsonPropertyName("bodyTemp")]
    public decimal? BodyTemp { get; set; }

    [JsonPropertyName("rapidHbsAgNegative")]
    public bool? RapidHbsAgNegative { get; set; }

    [JsonPropertyName("clinicalNotes")]
    public string? ClinicalNotes { get; set; }

    [JsonPropertyName("screeningStatus")]
    public string ScreeningStatus { get; set; } = "PENDING";

    [JsonPropertyName("deferralReason")]
    public string? DeferralReason { get; set; }

    [JsonPropertyName("targetVolumeMl")]
    public string? TargetVolumeMl { get; set; }

    [JsonPropertyName("doctorId")]
    public int? DoctorId { get; set; }

    [JsonPropertyName("doctorName")]
    public string? DoctorName { get; set; }

    [JsonPropertyName("screenedAt")]
    public DateTime? ScreenedAt { get; set; }
}
