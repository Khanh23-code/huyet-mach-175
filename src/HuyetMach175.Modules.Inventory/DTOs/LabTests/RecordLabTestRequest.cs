using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.DTOs.LabTests;

public class RecordLabTestRequest
{
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("bagId")]
    public string? BagId { get; set; }

    [Required(ErrorMessage = "Kết quả xét nghiệm HIV là bắt buộc.")]
    [JsonPropertyName("hivResult")]
    public TestResult HivResult { get; set; }

    [Required(ErrorMessage = "Kết quả xét nghiệm HBV là bắt buộc.")]
    [JsonPropertyName("hbvResult")]
    public TestResult HbvResult { get; set; }

    [Required(ErrorMessage = "Kết quả xét nghiệm HCV là bắt buộc.")]
    [JsonPropertyName("hcvResult")]
    public TestResult HcvResult { get; set; }

    [Required(ErrorMessage = "Kết quả xét nghiệm Giang mai là bắt buộc.")]
    [JsonPropertyName("syphilisResult")]
    public TestResult SyphilisResult { get; set; }

    [JsonPropertyName("irregularAntibody")]
    public TestResult? IrregularAntibody { get; set; }

    [JsonPropertyName("malariaResult")]
    public TestResult? MalariaResult { get; set; }

    [Required(ErrorMessage = "Nhóm máu ABO khẳng định là bắt buộc.")]
    [JsonPropertyName("confirmedBloodType")]
    public BloodType ConfirmedBloodType { get; set; }

    [Required(ErrorMessage = "Yếu tố Rh khẳng định là bắt buộc.")]
    [JsonPropertyName("confirmedRhFactor")]
    public RhFactor ConfirmedRhFactor { get; set; }

    [JsonPropertyName("confirmedRh")]
    public RhFactor? ConfirmedRh
    {
        get => ConfirmedRhFactor;
        set { if (value.HasValue) ConfirmedRhFactor = value.Value; }
    }
}