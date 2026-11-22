using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Sessions;

public class CollectionResponse
{
    [JsonPropertyName("sessionId")]
    public int SessionId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("actualVolumeMl")]
    public int? ActualVolumeMl { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; }

    [JsonPropertyName("bloodBagId")]
    public string? BloodBagId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}