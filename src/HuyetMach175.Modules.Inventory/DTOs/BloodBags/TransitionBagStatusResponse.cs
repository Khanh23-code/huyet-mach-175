using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class TransitionBagStatusResponse
{
    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("previousStatus")]
    public string PreviousStatus { get; set; } = string.Empty;

    [JsonPropertyName("previousStatusLabel")]
    public string PreviousStatusLabel { get; set; } = string.Empty;

    [JsonPropertyName("currentStatus")]
    public string CurrentStatus { get; set; } = string.Empty;

    [JsonPropertyName("currentStatusLabel")]
    public string CurrentStatusLabel { get; set; } = string.Empty;

    [JsonPropertyName("storageLocation")]
    public StorageLocationSummaryDto? StorageLocation { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

