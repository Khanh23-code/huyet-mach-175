using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;

namespace HuyetMach175.Modules.Donation.Services;

public interface ICampaignsService
{
    Task<PagedResult<CampaignListItemDto>> GetCampaignsAsync(CampaignFilterDto filter);
    Task<CampaignDetailDto> GetCampaignByIdAsync(int id);
    Task<CampaignDetailDto> CreateCampaignAsync(CreateCampaignRequest request, int userId);
    Task<CampaignDetailDto> UpdateCampaignAsync(int id, UpdateCampaignRequest request);
    Task<CampaignDetailDto> SubmitForApprovalAsync(int id);
    Task<CampaignDetailDto> ApproveCampaignAsync(int id, ApproveCampaignRequest request, int userId);
    Task<CampaignDetailDto> UpdateCampaignStatusAsync(int id, UpdateCampaignStatusRequest request, int userId);
    Task DeleteCampaignAsync(int id);
}
