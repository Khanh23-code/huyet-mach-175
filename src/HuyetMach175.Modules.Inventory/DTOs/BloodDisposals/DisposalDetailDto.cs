using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class DisposalDetailDto : DisposalRecordDto
{
    [JsonPropertyName("storageLocationId")]
    public int? StorageLocationId { get; set; }

    [JsonPropertyName("storageLocationName")]
    public string? StorageLocationName { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime? CollectedAt { get; set; }

    [JsonPropertyName("expiredAt")]
    public DateTime? ExpiredAt { get; set; }

    [JsonPropertyName("currentBagStatus")]
    public string CurrentBagStatus { get; set; } = string.Empty;

    [JsonPropertyName("currentBagStatusText")]
    public string CurrentBagStatusText { get; set; } = string.Empty;

    [JsonPropertyName("discardReason")]
    public string? DiscardReason { get; set; }
}