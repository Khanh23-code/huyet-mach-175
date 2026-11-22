using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class AppointmentListItemDto
{
    public int AppointmentId { get; set; }
    public int DonorId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorCitizenId { get; set; } = string.Empty;
    public string DonorPhoneNumber { get; set; } = string.Empty;
    public DateOnly DonorDateOfBirth { get; set; }
    public string DonorGender { get; set; } = string.Empty;
    public BloodType? DonorBloodType { get; set; }
    public RhFactor? DonorRhFactor { get; set; }
    public int? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public int? SlotId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string QrCodeToken { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public bool IsWalkIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool HasPreScreeningSurvey { get; set; }
    public int? SurveyRiskScore { get; set; }
    public bool? SurveyHasRisk { get; set; }
}
