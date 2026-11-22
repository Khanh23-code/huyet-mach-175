using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Processing;

public class SeparateBloodBagResponse
{
    [JsonPropertyName("parentBagId")]
    public int ParentBagId { get; set; }

    [JsonPropertyName("parentBarcode")]
    public string ParentBarcode { get; set; } = string.Empty;

    [JsonPropertyName("parentStatus")]
    public string ParentStatus { get; set; } = string.Empty;

    [JsonPropertyName("processedAt")]
    public DateTime ProcessedAt { get; set; }

    [JsonPropertyName("technicianId")]
    public int TechnicianId { get; set; }

    [JsonPropertyName("technicianName")]
    public string TechnicianName { get; set; } = string.Empty;

    [JsonPropertyName("derivedComponents")]
    public List<DerivedComponentDto> DerivedComponents { get; set; } = new List<DerivedComponentDto>();

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class DerivedComponentDto
{
    [JsonPropertyName("bagId")]
    public int BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeCode")]
    public string ComponentTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeName")]
    public string ComponentTypeName { get; set; } = string.Empty;

    [JsonPropertyName("bloodType")]
    public string BloodType { get; set; } = string.Empty;

    [JsonPropertyName("rhFactor")]
    public string RhFactor { get; set; } = string.Empty;

    [JsonPropertyName("bloodGroup")]
    public string BloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("volumeMl")]
    public int VolumeMl { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("storageLocationId")]
    public int StorageLocationId { get; set; }

    [JsonPropertyName("storageLocationName")]
    public string StorageLocationName { get; set; } = string.Empty;

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; set; }
}