using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class BloodBagScanResponse
{
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

    [JsonPropertyName("componentType")]
    public string ComponentType { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeCode")]
    public string ComponentTypeCode { get; set; } = string.Empty;

    [JsonPropertyName("volumeMl")]
    public int VolumeMl { get; set; }

    [JsonPropertyName("currentStatus")]
    public string CurrentStatus { get; set; } = string.Empty;

    [JsonPropertyName("currentStatusLabel")]
    public string CurrentStatusLabel { get; set; } = string.Empty;

    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; }

    [JsonPropertyName("nextStatus")]
    public string? NextStatus { get; set; }

    [JsonPropertyName("nextStatusLabel")]
    public string? NextStatusLabel { get; set; }

    [JsonPropertyName("nextStep")]
    public int? NextStep { get; set; }

    [JsonPropertyName("isLocked")]
    public bool IsLocked { get; set; }

    [JsonPropertyName("isDiscarded")]
    public bool IsDiscarded { get; set; }

    [JsonPropertyName("isExpired")]
    public bool IsExpired { get; set; }

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonPropertyName("storageLocation")]
    public StorageLocationSummaryDto? StorageLocation { get; set; }

    [JsonPropertyName("timeline")]
    public List<BloodBagTimelineStepDto> Timeline { get; set; } = new();
}

public class StorageLocationSummaryDto
{
    [JsonPropertyName("locationId")]
    public int LocationId { get; set; }

    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = string.Empty;
}

public class BloodBagTimelineStepDto
{
    [JsonPropertyName("step")]
    public int Step { get; set; }

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }
}

