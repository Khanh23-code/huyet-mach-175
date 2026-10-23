using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Donors;

public class DonorHistoryItemDto
{
    public int AppointmentId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public AppointmentStatus Status { get; set; }
    public bool IsWalkIn { get; set; }
    public int? ActualVolumeMl { get; set; }
    public SessionStatus? SessionStatus { get; set; }
    public ScreeningStatus? ScreeningStatus { get; set; }
    public string? DeferralReason { get; set; }
    public DateTime? CompletedAt { get; set; }
}