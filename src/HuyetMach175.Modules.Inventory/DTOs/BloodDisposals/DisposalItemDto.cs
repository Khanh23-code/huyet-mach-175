using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class DisposalItemDto
{
    [JsonPropertyName("itemId")]
    public int ItemId { get; set; }

    [JsonPropertyName("bagId")]
    public int BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("bloodType")]
    public string BloodType { get; set; } = string.Empty;

    [JsonPropertyName("rhFactor")]
    public string RhFactor { get; set; } = string.Empty;

    [JsonPropertyName("bloodGroup")]
    public string BloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeCode")]
    public string ComponentTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeName")]
    public string ComponentTypeName { get; set; } = string.Empty;

    [JsonPropertyName("volumeMl")]
    public int VolumeMl { get; set; }

    [JsonPropertyName("storageLocationId")]
    public int? StorageLocationId { get; set; }

    [JsonPropertyName("storageLocationName")]
    public string? StorageLocationName { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime? CollectedAt { get; set; }

    [JsonPropertyName("expiredAt")]
    public DateTime? ExpiredAt { get; set; }

    [JsonPropertyName("previousStatus")]
    public string PreviousStatus { get; set; } = string.Empty;

    [JsonPropertyName("previousStatusText")]
    public string PreviousStatusText { get; set; } = string.Empty;

    [JsonPropertyName("currentBagStatus")]
    public string CurrentBagStatus { get; set; } = string.Empty;

    [JsonPropertyName("currentBagStatusText")]
    public string CurrentBagStatusText { get; set; } = string.Empty;

    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; set; }

    [JsonPropertyName("verifiedAt")]
    public DateTime? VerifiedAt { get; set; }
}

