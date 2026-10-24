using HuyetMach175.Modules.Donation.DTOs.Sites;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.Controllers;

[ApiController]
[Route("api/sites")]
[Authorize]
[Tags("Donation.Sites")]
public class SitesController : ControllerBase
{
    private readonly ISitesService _sitesService;

    public SitesController(ISitesService sitesService)
    {
        _sitesService = sitesService;
    }

    [HttpGet]
    [HasPermission("CAMPAIGN_VIEW")]
    [EndpointSummary("Danh sách điểm hiến máu đang phát hành")]
    [EndpointDescription(
        "Trả về danh sách điểm hiến (kèm khung giờ và sức chứa mỗi slot). Mặc định chỉ lấy điểm đang hoạt động.\n\n" +
        "**Quyền:** CAMPAIGN_VIEW (MGT, BBNK, DON).")]
    [ProducesResponseType(typeof(List<SiteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSites([FromQuery] bool? isActive = true, [FromQuery] string? keyword = null)
    {
        return Ok(await _sitesService.GetSitesAsync(isActive, keyword));
    }

    [HttpGet("{id:int}")]
    [HasPermission("CAMPAIGN_VIEW")]
    [EndpointSummary("Chi tiết một điểm hiến máu")]
    [ProducesResponseType(typeof(SiteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSiteById(int id)
    {
        return Ok(await _sitesService.GetSiteByIdAsync(id));
    }
}
