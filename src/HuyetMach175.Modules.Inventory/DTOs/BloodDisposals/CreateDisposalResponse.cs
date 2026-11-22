using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class CreateDisposalResponse
{
    [JsonPropertyName("disposalId")]
    public int DisposalId { get; set; }

    [JsonPropertyName("disposalCode")]
    public string DisposalCode { get; set; } = string.Empty;

    [JsonPropertyName("totalBags")]
    public int TotalBags { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("reasonText")]
    public string ReasonText { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; } = string.Empty;

    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

