using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class CampaignListItemDto
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public CampaignType CampaignType { get; set; } = CampaignType.EVENT;
    public string? OperatingDays { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TargetDonations { get; set; }
    public int? MaxCapacity { get; set; }
    public CampaignStatus Status { get; set; }
    public int TotalRegistered { get; set; }
    public int TotalCollected { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
