using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class UpdateClinicalScreeningRequest
{
    [JsonPropertyName("surveyAnswers")]
    public JsonElement? SurveyAnswers { get; set; }

    [JsonPropertyName("weightKg")]
    [Range(30, 250, ErrorMessage = "Cân nặng phải nằm trong khoảng từ 30kg đến 250kg.")]
    public decimal? WeightKg { get; set; }

    [JsonPropertyName("systolicBloodPressure")]
    [Range(50, 250, ErrorMessage = "Huyết áp tâm thu phải nằm trong khoảng từ 50 đến 250 mmHg.")]
    public int? SystolicBloodPressure { get; set; }

    [JsonPropertyName("diastolicBloodPressure")]
    [Range(30, 150, ErrorMessage = "Huyết áp tâm trương phải nằm trong khoảng từ 30 đến 150 mmHg.")]
    public int? DiastolicBloodPressure { get; set; }

    [JsonPropertyName("pulse")]
    [Range(30, 200, ErrorMessage = "Nhịp tim/mạch phải nằm trong khoảng từ 30 đến 200 nhịp/phút.")]
    public int? Pulse { get; set; }

    [JsonPropertyName("hemoglobinLevel")]
    [Range(50, 250, ErrorMessage = "Nồng độ Hemoglobin phải nằm trong khoảng từ 50 đến 250 g/L.")]
    public decimal? HemoglobinLevel { get; set; }

    [JsonPropertyName("bodyTemp")]
    [Range(34, 42, ErrorMessage = "Nhiệt độ cơ thể phải nằm trong khoảng từ 34°C đến 42°C.")]
    public decimal? BodyTemp { get; set; }

    [JsonPropertyName("rapidHbsAgNegative")]
    public bool? RapidHbsAgNegative { get; set; }

    [JsonPropertyName("clinicalNotes")]
    [MaxLength(500, ErrorMessage = "Ghi chú lâm sàng không được vượt quá 500 ký tự.")]
    public string? ClinicalNotes { get; set; }

    [Required(ErrorMessage = "Kết luận sàng lọc (screeningStatus) là bắt buộc.")]
    [JsonPropertyName("screeningStatus")]
    public ScreeningStatus ScreeningStatus { get; set; }

    [JsonPropertyName("deferralReason")]
    [MaxLength(500, ErrorMessage = "Lý do tạm hoãn/từ chối không được vượt quá 500 ký tự.")]
    public string? DeferralReason { get; set; }

    [JsonPropertyName("targetVolumeMl")]
    public TargetVolume? TargetVolumeMl { get; set; }
}

