using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class RegisterExternalBloodBagResponse
{
    [JsonPropertyName("bagId")] public int BagId { get; set; }
    [JsonPropertyName("barcode")] public string Barcode { get; set; } = string.Empty;
    [JsonPropertyName("componentTypeName")] public string ComponentTypeName { get; set; } = string.Empty;
    [JsonPropertyName("bloodGroup")] public string BloodGroup { get; set; } = string.Empty;
    [JsonPropertyName("volumeMl")] public int VolumeMl { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("externalUnitName")] public string ExternalUnitName { get; set; } = string.Empty;
    [JsonPropertyName("externalCode")] public string ExternalCode { get; set; } = string.Empty;
    [JsonPropertyName("storageLocationId")] public int StorageLocationId { get; set; }
    [JsonPropertyName("storageLocationName")] public string StorageLocationName { get; set; } = string.Empty;
    [JsonPropertyName("receivedTemperature")] public decimal ReceivedTemperature { get; set; }
    [JsonPropertyName("collectedAt")] public DateTime CollectedAt { get; set; }
    [JsonPropertyName("expiredAt")] public DateTime ExpiredAt { get; set; }
    [JsonPropertyName("receivedAt")] public DateTime ReceivedAt { get; set; }
    [JsonPropertyName("receivedBy")] public int ReceivedBy { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}