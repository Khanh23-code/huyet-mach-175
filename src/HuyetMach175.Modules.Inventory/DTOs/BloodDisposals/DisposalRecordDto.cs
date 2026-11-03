using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class DisposalRecordDto
{
    [JsonPropertyName("disposalId")]
    public int DisposalId { get; set; }

    [JsonPropertyName("disposalCode")]
    public string DisposalCode { get; set; } = string.Empty;

    [JsonPropertyName("totalBags")]
    public int TotalBags { get; set; }

    [JsonPropertyName("barcodesSummary")]
    public string BarcodesSummary { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("reasonText")]
    public string ReasonText { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; } = string.Empty;

    [JsonPropertyName("requestedBy")]
    public int RequestedBy { get; set; }

    [JsonPropertyName("requestedByName")]
    public string RequestedByName { get; set; } = string.Empty;

    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }

    [JsonPropertyName("approvedBy")]
    public int? ApprovedBy { get; set; }

    [JsonPropertyName("approvedByName")]
    public string? ApprovedByName { get; set; }

    [JsonPropertyName("approvedAt")]
    public DateTime? ApprovedAt { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }
}