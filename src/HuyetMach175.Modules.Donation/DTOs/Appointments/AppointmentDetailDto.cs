using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class AppointmentDetailDto
{
    public int AppointmentId { get; set; }
    public int DonorId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorCitizenId { get; set; } = string.Empty;
    public string DonorPhoneNumber { get; set; } = string.Empty;
    public string? DonorEmail { get; set; }
    public DateOnly DonorDateOfBirth { get; set; }
    public string DonorGender { get; set; } = string.Empty;
    public BloodType? DonorBloodType { get; set; }
    public RhFactor? DonorRhFactor { get; set; }
    public int TotalDonations { get; set; }
    public DateOnly? LastDonationDate { get; set; }

    public int? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public string? CampaignLocation { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string QrCodeToken { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public bool IsWalkIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Thông tin khảo sát sàng lọc trước (Pre-screening Survey)
    public bool HasPreScreeningSurvey { get; set; }
    public int? SurveyId { get; set; }
    public int? SurveyRiskScore { get; set; }
    public bool? SurveyHasRisk { get; set; }
    public string? SurveyAnswers { get; set; }
    public DateTime? SurveySubmittedAt { get; set; }
}

