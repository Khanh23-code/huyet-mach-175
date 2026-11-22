using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Sessions;

public class StartCollectionResponse
{
    [JsonPropertyName("sessionId")] public int SessionId { get; set; }
    [JsonPropertyName("bagId")] public int BagId { get; set; }
    [JsonPropertyName("barcode")] public string Barcode { get; set; } = string.Empty;
    [JsonPropertyName("bloodGroup")] public string BloodGroup { get; set; } = string.Empty;
    [JsonPropertyName("volumeMl")] public int VolumeMl { get; set; }
    [JsonPropertyName("collectedAt")] public DateTime CollectedAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("isNewBag")] public bool IsNewBag { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}