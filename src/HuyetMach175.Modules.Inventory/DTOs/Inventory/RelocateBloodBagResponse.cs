using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Inventory;

public class RelocateBloodBagResponse
{
    [JsonPropertyName("bagId")]
    public int BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("bloodGroup")]
    public string BloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeName")]
    public string ComponentTypeName { get; set; } = string.Empty;

    [JsonPropertyName("previousStorageLocationId")]
    public int? PreviousStorageLocationId { get; set; }

    [JsonPropertyName("previousStorageLocationName")]
    public string? PreviousStorageLocationName { get; set; }

    [JsonPropertyName("newStorageLocationId")]
    public int NewStorageLocationId { get; set; }

    [JsonPropertyName("newStorageLocationName")]
    public string NewStorageLocationName { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("relocatedAt")]
    public DateTime RelocatedAt { get; set; }

    [JsonPropertyName("relocatedBy")]
    public int? RelocatedBy { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}