using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;

namespace HuyetMach175.Modules.Donation.Services;

public interface ICampaignsService
{
    Task<PagedResult<CampaignListItemDto>> GetCampaignsAsync(CampaignFilterDto filter, Guid? createdBy = null);
    Task<PagedResult<CampaignListItemDto>> GetMyCampaignsAsync(CampaignFilterDto filter, Guid userId);
    Task<CampaignDetailDto> GetCampaignByIdAsync(Guid id);
    Task<CampaignSummaryDto> GetCampaignSummaryAsync(Guid id);
    Task<CampaignDetailDto> CreateCampaignAsync(CreateCampaignRequest request, Guid userId);
    Task<CampaignDetailDto> UpdateCampaignAsync(Guid id, UpdateCampaignRequest request);
    Task<CampaignDetailDto> SubmitForApprovalAsync(Guid id);
    Task<CampaignDetailDto> ApproveCampaignAsync(Guid id, ApproveCampaignRequest request, Guid userId);
    Task<CampaignDetailDto> CancelCampaignAsync(Guid id, CancelCampaignRequest request, Guid userId);
    Task<CampaignDetailDto> UpdateCampaignStatusAsync(Guid id, UpdateCampaignStatusRequest request, Guid userId);
    Task DeleteCampaignAsync(Guid id);
}