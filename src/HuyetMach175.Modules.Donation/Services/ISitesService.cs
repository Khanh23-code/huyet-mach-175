using HuyetMach175.Modules.Donation.DTOs.Sites;

namespace HuyetMach175.Modules.Donation.Services;

public interface ISitesService
{
    Task<List<SiteDto>> GetSitesAsync(bool? isActive, string? keyword);
    Task<SiteDto> GetSiteByIdAsync(int id);
}
