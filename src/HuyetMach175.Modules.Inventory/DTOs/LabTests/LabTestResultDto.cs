using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.LabTests;

public class LabTestResultDto
{
    [JsonPropertyName("testId")]
    public int TestId { get; set; }

    [JsonPropertyName("bagId")]
    public int BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("hivResult")]
    public string HivResult { get; set; } = string.Empty;

    [JsonPropertyName("hbvResult")]
    public string HbvResult { get; set; } = string.Empty;

    [JsonPropertyName("hcvResult")]
    public string HcvResult { get; set; } = string.Empty;

    [JsonPropertyName("syphilisResult")]
    public string SyphilisResult { get; set; } = string.Empty;

    [JsonPropertyName("irregularAntibody")]
    public string IrregularAntibody { get; set; } = string.Empty;

    [JsonPropertyName("confirmedBloodType")]
    public string ConfirmedBloodType { get; set; } = string.Empty;

    [JsonPropertyName("confirmedRhFactor")]
    public string ConfirmedRhFactor { get; set; } = string.Empty;

    [JsonPropertyName("confirmedBloodGroup")]
    public string ConfirmedBloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("overallConclusion")]
    public string OverallConclusion { get; set; } = string.Empty;

    [JsonPropertyName("newBagStatus")]
    public string NewBagStatus { get; set; } = string.Empty;

    [JsonPropertyName("discardReason")]
    public string? DiscardReason { get; set; }

    [JsonPropertyName("technicianId")]
    public int TechnicianId { get; set; }

    [JsonPropertyName("technicianName")]
    public string TechnicianName { get; set; } = string.Empty;

    [JsonPropertyName("testedAt")]
    public DateTime TestedAt { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}