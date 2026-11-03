using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class DisposalRecordDto
{
    [JsonPropertyName("disposalId")]
    public int DisposalId { get; set; }

    [JsonPropertyName("disposalCode")]
    public string DisposalCode { get; set; } = string.Empty;

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

    [JsonPropertyName("previousStatus")]
    public string PreviousStatus { get; set; } = string.Empty;

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