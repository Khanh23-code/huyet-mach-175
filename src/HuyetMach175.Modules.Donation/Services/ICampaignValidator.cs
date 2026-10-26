using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.Services;

public interface ICampaignValidator
{
    void ValidateDates(DateOnly startDate, DateOnly endDate, bool isNew = false);
    void ValidateTargetDonations(int targetDonations);
    void ValidateEditable(DonationCampaign campaign);
    void ValidateSubmittable(DonationCampaign campaign);
    void ValidateApprovable(DonationCampaign campaign, bool isApproved, string? rejectionReason);
    void ValidateCancellable(DonationCampaign campaign, string reason);
    void ValidateStatusTransition(DonationCampaign campaign, CampaignStatus targetStatus);
    void ValidateDeletable(DonationCampaign campaign);
}