namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class SurveyQueueItemDto
{
    public Guid AppointmentId { get; set; }
    public Guid DonorId { get; set; }
    public string DonorCode { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public DateTime? CheckedInAt { get; set; }
    public bool IsWalkIn { get; set; }
    public bool HasSubmittedSurvey { get; set; }
    public bool HasRisk { get; set; }
    public string ScreeningStatus { get; set; } = string.Empty;
}