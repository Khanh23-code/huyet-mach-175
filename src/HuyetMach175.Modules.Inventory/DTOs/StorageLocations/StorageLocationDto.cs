using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.StorageLocations;

public class StorageLocationDto
{
    [JsonPropertyName("locationId")]
    public int LocationId { get; set; }

    [JsonPropertyName("refrigeratorName")]
    public string RefrigeratorName { get; set; } = string.Empty;

    [JsonPropertyName("shelfNumber")]
    public string ShelfNumber { get; set; } = string.Empty;

    [JsonPropertyName("targetTemperature")]
    public decimal TargetTemperature { get; set; }

    [JsonPropertyName("isFull")]
    public bool IsFull { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("currentBagsCount")]
    public int CurrentBagsCount { get; set; }
}
