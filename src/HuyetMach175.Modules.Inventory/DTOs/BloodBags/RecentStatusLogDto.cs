using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class RecentStatusLogDto
{
    [JsonPropertyName("logId")]
    public int LogId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("bloodGroup")]
    public string BloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("previousStatus")]
    public string PreviousStatus { get; set; } = string.Empty;

    [JsonPropertyName("previousStatusName")]
    public string PreviousStatusName { get; set; } = string.Empty;

    [JsonPropertyName("newStatus")]
    public string NewStatus { get; set; } = string.Empty;

    [JsonPropertyName("newStatusName")]
    public string NewStatusName { get; set; } = string.Empty;

    [JsonPropertyName("performedBy")]
    public string PerformedBy { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class RecentStatusLogsResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<RecentStatusLogDto> Items { get; set; } = new();
}

