using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Processing;

public class ProcessingQueueItemDto
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

    [JsonPropertyName("bloodType")]
    public string BloodType { get; set; } = string.Empty;

    [JsonPropertyName("rhFactor")]
    public string RhFactor { get; set; } = string.Empty;

    [JsonPropertyName("bloodGroup")]
    public string BloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("storageLocationId")]
    public int? StorageLocationId { get; set; }

    [JsonPropertyName("storageLocationName")]
    public string? StorageLocationName { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonPropertyName("testedAt")]
    public DateTime? TestedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}