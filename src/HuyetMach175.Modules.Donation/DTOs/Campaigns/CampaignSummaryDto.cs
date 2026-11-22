using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class CampaignSummaryDto
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public CampaignType CampaignType { get; set; }
    public string? OperatingDays { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public CampaignStatus Status { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    public int TargetDonations { get; set; }
    public int TotalCollectedUnits { get; set; }
    public int ShortfallUnits { get; set; }
    public int ExceededUnits { get; set; }
    public double CompletionRate { get; set; }
    public int TotalVolumeMl { get; set; }

    public int TotalRegistered { get; set; }
    public int TotalCheckedIn { get; set; }
    public int ScheduledCheckedIn { get; set; }
    public int WalkInCheckedIn { get; set; }
    public int PendingArrival { get; set; }
    public int NoShowCount { get; set; }
    public int CancelledCount { get; set; }
    public double AttendanceRate { get; set; }

    public int TotalScreened { get; set; }
    public int QualifiedCount { get; set; }
    public int DeferredCount { get; set; }
    public int UnqualifiedCount { get; set; }
    public int PendingScreeningCount { get; set; }
    public List<CampaignDeferralReasonStatDto> TopDeferralReasons { get; set; } = new List<CampaignDeferralReasonStatDto>();

    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int AbortedSessions { get; set; }
    public int WaitingCollectionSessions { get; set; }
    public int IncidentCount { get; set; }
    public List<CampaignIncidentDto> Incidents { get; set; } = new List<CampaignIncidentDto>();

    public List<CampaignVolumeStatDto> VolumeBreakdown { get; set; } = new List<CampaignVolumeStatDto>();
    public List<CampaignBloodTypeStatDto> BloodTypeBreakdown { get; set; } = new List<CampaignBloodTypeStatDto>();
    public List<CampaignRhStatDto> RhBreakdown { get; set; } = new List<CampaignRhStatDto>();
    public List<CampaignSlotSummaryDto> Slots { get; set; } = new List<CampaignSlotSummaryDto>();
}

public class CampaignDeferralReasonStatDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CampaignIncidentDto
{
    public int SessionId { get; set; }
    public int AppointmentId { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorCitizenId { get; set; } = string.Empty;
    public string TargetVolumeMl { get; set; } = string.Empty;
    public int? ActualVolumeMl { get; set; }
    public string IncidentDescription { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}

public class CampaignVolumeStatDto
{
    public string VolumeType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public int Count { get; set; }
    public int TotalMl { get; set; }
}

public class CampaignBloodTypeStatDto
{
    public string BloodType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class CampaignRhStatDto
{
    public string RhFactor { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class CampaignSlotSummaryDto
{
    public int SlotId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Capacity { get; set; }
    public int TotalRegistered { get; set; }
    public int TotalCheckedIn { get; set; }
    public int TotalCollected { get; set; }
    public int TotalVolumeMl { get; set; }
}

