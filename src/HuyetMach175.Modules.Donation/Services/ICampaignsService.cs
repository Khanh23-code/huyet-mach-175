using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;

namespace HuyetMach175.Modules.Donation.Services;

public interface ICampaignsService
{
    Task<PagedResult<CampaignListItemDto>> GetCampaignsAsync(CampaignFilterDto filter);
    Task<CampaignDetailDto> GetCampaignByIdAsync(int id);
    Task<CampaignDetailDto> CreateCampaignAsync(CreateCampaignRequest request, int userId);
    Task<CampaignDetailDto> UpdateCampaignAsync(int id, UpdateCampaignRequest request);
}