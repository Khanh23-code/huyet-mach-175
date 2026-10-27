using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class SubmitOnlineSurveyResponse
{
    [JsonPropertyName("surveyId")]
    public int SurveyId { get; set; }

    [JsonPropertyName("appointmentId")]
    public int AppointmentId { get; set; }

    [JsonPropertyName("hasRisk")]
    public bool HasRisk { get; set; }

    [JsonPropertyName("riskScore")]
    public int RiskScore { get; set; }

    [JsonPropertyName("submittedAt")]
    public DateTime SubmittedAt { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "Nộp bảng khảo sát sức khỏe thành công";
}