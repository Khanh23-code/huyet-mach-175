using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class DisposalCandidateDto
{
    [JsonPropertyName("bagId")]
    public int BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("bloodGroup")]
    public string BloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeCode")]
    public string ComponentTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeName")]
    public string ComponentTypeName { get; set; } = string.Empty;

    [JsonPropertyName("volumeMl")]
    public int VolumeMl { get; set; }

    [JsonPropertyName("storageLocationName")]
    public string? StorageLocationName { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; set; }

    [JsonPropertyName("discardReason")]
    public string? DiscardReason { get; set; }
}