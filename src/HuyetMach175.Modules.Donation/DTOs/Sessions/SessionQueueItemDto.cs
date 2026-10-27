using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Sessions;

public class SessionQueueItemDto
{
    [JsonPropertyName("sessionId")]
    public int SessionId { get; set; }

    [JsonPropertyName("appointmentId")]
    public int AppointmentId { get; set; }

    [JsonPropertyName("donorCode")]
    public string DonorCode { get; set; } = string.Empty;

    [JsonPropertyName("donorName")]
    public string DonorName { get; set; } = string.Empty;

    [JsonPropertyName("targetVolumeMl")]
    public string TargetVolumeMl { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("screenedAt")]
    public DateTime? ScreenedAt { get; set; }

    [JsonPropertyName("doctorName")]
    public string? DoctorName { get; set; }
}