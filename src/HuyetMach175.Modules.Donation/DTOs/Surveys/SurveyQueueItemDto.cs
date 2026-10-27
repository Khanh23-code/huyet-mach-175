using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Surveys;

public class SurveyQueueItemDto
{
    public int AppointmentId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public int? SlotId { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string QrCodeToken { get; set; } = string.Empty;
    public bool IsWalkIn { get; set; }
    public DateTime? CheckedInAt { get; set; }

    public int? CampaignId { get; set; }
    public string? CampaignName { get; set; }

    public int DonorId { get; set; }
    public string DonorCode { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public string DonorCitizenId { get; set; } = string.Empty;
    public string DonorPhoneNumber { get; set; } = string.Empty;
    public DateOnly DonorDateOfBirth { get; set; }
    public string DonorGender { get; set; } = string.Empty;
    public BloodType? DonorBloodType { get; set; }
    public RhFactor? DonorRhFactor { get; set; }
    public int TotalDonations { get; set; }
    public DateOnly? LastDonationDate { get; set; }

    public int? SurveyId { get; set; }
    public bool HasSubmittedSurvey { get; set; }
    public int RiskScore { get; set; }
    public bool HasRisk { get; set; }
    public DateTime? SurveySubmittedAt { get; set; }
    public ScreeningStatus ScreeningStatus { get; set; } = ScreeningStatus.PENDING;
    public TargetVolume? TargetVolumeMl { get; set; }
    public DateTime? ScreenedAt { get; set; }
}