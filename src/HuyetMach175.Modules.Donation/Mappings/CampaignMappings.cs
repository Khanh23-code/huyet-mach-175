using System.Linq.Expressions;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Campaigns;

namespace HuyetMach175.Modules.Donation.Mappings;

public static class CampaignMappings
{
    public static readonly Expression<Func<DonationCampaign, CampaignListItemDto>> ProjectToListItem = c => new CampaignListItemDto
    {
        CampaignId = c.CampaignId,
        CampaignName = c.CampaignName,
        Description = c.Description,
        LocationName = c.LocationName,
        CampaignType = c.CampaignType,
        OperatingDays = c.OperatingDays,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        TargetDonations = c.TargetDonations,
        MaxCapacity = c.MaxCapacity,
        Status = c.Status,
        TotalRegistered = c.Appointments.Count(a => a.Status != AppointmentStatus.CANCELLED),
        TotalCollected = c.Appointments.Count(a => a.DonationSession != null && a.DonationSession.Status == SessionStatus.COMPLETED),
        RejectionReason = c.RejectionReason,
        CancellationReason = c.CancellationReason,
        CreatedBy = c.CreatedBy,
        CreatedByName = c.CreatedByUser != null ? c.CreatedByUser.FullName : string.Empty,
        CreatedAt = c.CreatedAt
    };

    public static CampaignDetailDto ToDetailDto(this DonationCampaign campaign)
    {
        var totalRegistered = campaign.Appointments.Count(a => a.Status != AppointmentStatus.CANCELLED);
        var totalCheckedIn = campaign.Appointments.Count(a => a.CheckedInAt != null || a.Status == AppointmentStatus.CHECKED_IN);
        var totalCollected = campaign.Appointments.Count(a => a.DonationSession != null && a.DonationSession.Status == SessionStatus.COMPLETED);
        var completionRate = campaign.TargetDonations > 0
            ? Math.Round((double)totalCollected / campaign.TargetDonations * 100, 2)
            : 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var remainingDays = campaign.EndDate >= today ? campaign.EndDate.DayNumber - today.DayNumber : 0;

        return new CampaignDetailDto
        {
            CampaignId = campaign.CampaignId,
            CampaignName = campaign.CampaignName,
            Description = campaign.Description,
            LocationName = campaign.LocationName,
            CampaignType = campaign.CampaignType,
            OperatingDays = campaign.OperatingDays,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            TargetDonations = campaign.TargetDonations,
            MaxCapacity = campaign.MaxCapacity,
            Status = campaign.Status,
            CreatedBy = campaign.CreatedBy,
            CreatedByName = campaign.CreatedByUser != null ? campaign.CreatedByUser.FullName : string.Empty,
            ApprovedBy = campaign.ApprovedBy,
            ApprovedByName = campaign.ApprovedByUser != null ? campaign.ApprovedByUser.FullName : null,
            ApprovedAt = campaign.ApprovedAt,
            CancelledBy = campaign.CancelledBy,
            CancelledByName = campaign.CancelledByUser != null ? campaign.CancelledByUser.FullName : null,
            CancelledAt = campaign.CancelledAt,
            CancellationReason = campaign.CancellationReason,
            RejectionReason = campaign.RejectionReason,
            CreatedAt = campaign.CreatedAt,
            TotalRegistered = totalRegistered,
            TotalCheckedIn = totalCheckedIn,
            TotalCollected = totalCollected,
            CompletionRate = completionRate,
            RemainingDays = remainingDays,
            Slots = campaign.Slots
                .OrderBy(s => s.StartTime)
                .Select(s => new CampaignSlotDto
                {
                    SlotId = s.SlotId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Capacity = s.Capacity
                })
                .ToList()
        };
    }
}