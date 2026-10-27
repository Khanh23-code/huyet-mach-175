using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class ClinicalScreeningResponse
{
    [JsonPropertyName("appointmentId")]
    public int AppointmentId { get; set; }

    [JsonPropertyName("screeningStatus")]
    public string ScreeningStatus { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public int? SessionId { get; set; }

    [JsonPropertyName("sessionStatus")]
    public string? SessionStatus { get; set; }

    [JsonPropertyName("targetVolumeMl")]
    public string? TargetVolumeMl { get; set; }

    [JsonPropertyName("screenedAt")]
    public DateTime ScreenedAt { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

