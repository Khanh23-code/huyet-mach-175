using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize]
[Tags("Donation.Campaigns")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignsService _campaignsService;

    public CampaignsController(ICampaignsService campaignsService)
    {
        _campaignsService = campaignsService;
    }

    [HttpGet]
    [HasPermission("CAMPAIGN_VIEW")]
    [EndpointSummary("Lấy danh sách các đợt hiến máu")]
    [EndpointDescription(
        "Trả về danh sách các đợt hiến máu có hỗ trợ phân trang, tìm kiếm từ khóa theo tên chiến dịch hoặc địa điểm, " +
        "lọc theo trạng thái và lọc theo khoảng thời gian diễn ra đợt hiến máu.\n\n" +
        "Mỗi đợt hiến máu trong danh sách bao gồm số lượng người đã đăng ký (TotalRegistered) và số lượng túi máu đã thu gom được (TotalCollected).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_VIEW** (các vai trò như BBNK, MGT, DON, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách đợt hiến máu theo cấu trúc phân trang.\n" +
        "- 400 BadRequest: Dữ liệu phân trang hoặc khoảng thời gian không hợp lệ (PageNumber < 1, PageSize ngoài [1, 100], FromDate > ToDate).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập CAMPAIGN_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<CampaignListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCampaigns([FromQuery] CampaignFilterDto filter)
    {
        var result = await _campaignsService.GetCampaignsAsync(filter);
        return Ok(result);
    }
}