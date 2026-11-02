using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.LabTests;

public class LabTestQueueItemDto
{
    [JsonPropertyName("bagId")]
    public int BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("donorCode")]
    public string? DonorCode { get; set; }

    [JsonPropertyName("donorName")]
    public string? DonorName { get; set; }

    [JsonPropertyName("componentTypeCode")]
    public string ComponentTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeName")]
    public string ComponentTypeName { get; set; } = string.Empty;

    [JsonPropertyName("volumeMl")]
    public int VolumeMl { get; set; }

    [JsonPropertyName("expectedBloodType")]
    public string ExpectedBloodType { get; set; } = string.Empty;

    [JsonPropertyName("expectedRhFactor")]
    public string ExpectedRhFactor { get; set; } = string.Empty;

    [JsonPropertyName("expectedBloodGroup")]
    public string ExpectedBloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("storageLocationId")]
    public int? StorageLocationId { get; set; }

    [JsonPropertyName("storageLocationName")]
    public string? StorageLocationName { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}