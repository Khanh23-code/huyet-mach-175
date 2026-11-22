using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class SubmitOnlineSurveyRequest
{
    [Required(ErrorMessage = "Mã lịch hẹn (appointmentId) không được để trống.")]
    [JsonPropertyName("appointmentId")]
    public int AppointmentId { get; set; }

    [Required(ErrorMessage = "Bảng câu trả lời khảo sát (surveyAnswers) không được để trống.")]
    [JsonPropertyName("surveyAnswers")]
    public JsonElement SurveyAnswers { get; set; }
}