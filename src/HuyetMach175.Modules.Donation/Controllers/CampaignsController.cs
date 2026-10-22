using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
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

    [HttpGet("{id:int}")]
    [HasPermission("CAMPAIGN_VIEW")]
    [EndpointSummary("Xem chi tiết đợt hiến máu và thống kê tiến độ")]
    [EndpointDescription(
        "Trả về thông tin chi tiết một đợt hiến máu cùng thống kê tiến độ thu gom thực tế:\n" +
        "- Số lượng người hiến máu đã đăng ký lịch (TotalRegistered).\n" +
        "- Số người đã có mặt điểm danh (TotalCheckedIn).\n" +
        "- Số túi máu đã thu gom được (TotalCollected).\n" +
        "- Tỷ lệ hoàn thành mục tiêu % (CompletionRate).\n" +
        "- Số ngày còn lại đến khi kết thúc đợt (RemainingDays).\n" +
        "- Thông tin người tạo, người phê duyệt, người hủy (kèm thời gian tương ứng).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_VIEW** (vai trò BBNK, MGT, DON, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về thông tin chi tiết đợt hiến máu kèm thống kê tiến độ.\n" +
        "- 400 BadRequest: ID đợt hiến máu không hợp lệ (<= 0).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập CAMPAIGN_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampaignById(int id)
    {
        var result = await _campaignsService.GetCampaignByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission("CAMPAIGN_EDIT")]
    [EndpointSummary("Tạo mới đề xuất đợt hiến máu")]
    [EndpointDescription(
        "Nhân viên Ngân hàng máu (BBNK) hoặc Lãnh đạo (MGT) tạo một đề xuất đợt hiến máu mới. " +
        "Đợt hiến máu vừa tạo sẽ mặc định ở trạng thái Bản nháp (**DRAFT**).\n\n" +
        "Người tạo sẽ được tự động ghi nhận theo định danh của tài khoản đang đăng nhập.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_EDIT** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Tạo đề xuất đợt hiến máu thành công, trả về chi tiết đợt hiến máu vừa tạo.\n" +
        "- 400 BadRequest: Dữ liệu gửi lên không hợp lệ (ngày kết thúc trước ngày bắt đầu, ngày bắt đầu trong quá khứ, chỉ tiêu <= 0...).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_EDIT.")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        var userId = User.GetUserId();
        var result = await _campaignsService.CreateCampaignAsync(request, userId);
        return CreatedAtAction(nameof(GetCampaignById), new { id = result.CampaignId }, result);
    }

    [HttpPut("{id:int}")]
    [HasPermission("CAMPAIGN_EDIT")]
    [EndpointSummary("Cập nhật thông tin đợt hiến máu")]
    [EndpointDescription(
        "Chỉnh sửa thông tin cơ bản của đợt hiến máu (tên chiến dịch, địa điểm, ngày bắt đầu, ngày kết thúc, chỉ tiêu thu gom).\n\n" +
        "**Quy tắc nghiệp vụ:** Chỉ được phép chỉnh sửa khi đợt hiến máu đang ở trạng thái Bản nháp (**DRAFT**) hoặc Bị từ chối (**REJECTED**).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_EDIT** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Cập nhật đợt hiến máu thành công, trả về chi tiết đợt hiến máu sau khi cập nhật.\n" +
        "- 400 BadRequest: Đợt hiến máu không ở trạng thái DRAFT hoặc REJECTED, hoặc khoảng ngày/chỉ tiêu không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCampaign(int id, [FromBody] UpdateCampaignRequest request)
    {
        var result = await _campaignsService.UpdateCampaignAsync(id, request);
        return Ok(result);
    }
}