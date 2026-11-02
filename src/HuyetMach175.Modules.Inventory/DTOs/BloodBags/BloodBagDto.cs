using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class BloodBagDto
{
    [JsonPropertyName("bagId")]
    public int BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("parentBagId")]
    public int? ParentBagId { get; set; }

    [JsonPropertyName("parentBarcode")]
    public string? ParentBarcode { get; set; }

    [JsonPropertyName("componentTypeId")]
    public int ComponentTypeId { get; set; }

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
    public int? StorageLocationId { get; set; }

    [JsonPropertyName("storageLocationName")]
    public string? StorageLocationName { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

