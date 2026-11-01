using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Donors;
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
    private readonly IDonorImportService _donorImportService;

    public CampaignsController(ICampaignsService campaignsService, IDonorImportService donorImportService)
    {
        _campaignsService = campaignsService;
        _donorImportService = donorImportService;
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

    [HttpGet("mine")]
    [HasPermission("CAMPAIGN_VIEW")]
    [EndpointSummary("Lấy danh sách đợt hiến máu do chính mình tạo")]
    [EndpointDescription(
        "Trả về danh sách các đợt hiến máu do tài khoản hiện tại khởi tạo, hỗ trợ phân trang và bộ lọc như danh sách chung.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_VIEW** (các vai trò như BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách đợt hiến máu do chính mình tạo theo cấu trúc phân trang.\n" +
        "- 400 BadRequest: Dữ liệu phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập CAMPAIGN_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<CampaignListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyCampaigns([FromQuery] CampaignFilterDto filter)
    {
        var userId = User.GetUserId();
        var result = await _campaignsService.GetMyCampaignsAsync(filter, userId);
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

    [HttpGet("{id:int}/summary")]
    [HasPermission("CAMPAIGN_VIEW")]
    [EndpointSummary("Báo cáo tổng kết đợt hiến máu")]
    [EndpointDescription(
        "Tổng hợp toàn diện các chỉ số và kết quả của đợt hiến máu từ khi bắt đầu (IN_PROGRESS) cho đến khi kết thúc (COMPLETED) hoặc bị hủy (CANCELLED):\n" +
        "- Kết quả thực tế so với chỉ tiêu (Target, Collected, Shortfall, CompletionRate, TotalVolumeMl).\n" +
        "- Thống kê lịch hẹn và tiếp đón (Check-in, vãng lai, đặt trước, chưa đến, vắng mặt, hủy, tỷ lệ có mặt).\n" +
        "- Thống kê kết quả khám sàng lọc lâm sàng và lý do tạm hoãn.\n" +
        "- Thống kê phiên lấy máu, số ca thành công, số ca sự cố kèm chi tiết sự cố lâm sàng.\n" +
        "- Cơ cấu phân bổ thể tích túi máu (250ml, 350ml, 450ml) và nhóm máu.\n" +
        "- Thống kê chi tiết tiến độ theo từng khung giờ (Slot).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_VIEW** (các vai trò như BBNK, MGT, DON, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về báo cáo tổng kết chi tiết của đợt hiến máu.\n" +
        "- 400 BadRequest: ID không hợp lệ hoặc đợt hiến máu chưa bắt đầu (DRAFT, PENDING_APPROVAL).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập CAMPAIGN_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(CampaignSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampaignSummary(int id)
    {
        var result = await _campaignsService.GetCampaignSummaryAsync(id);
        return Ok(result);
    }

    [HttpGet("{id:int}/donors/import-template")]
    [HasPermission("CAMPAIGN_VIEW")]
    [EndpointSummary("Tải file Excel mẫu danh sách đăng ký hiến máu")]
    [EndpointDescription(
        "Tải về file Excel mẫu chuẩn (.xlsx) chứa thông tin của đợt hiến máu, cấu trúc cột thông tin người hiến, " +
        "khung giờ tiếp nhận tương ứng và 9 câu hỏi khảo sát online.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_VIEW** (BBNK, MGT, DON, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về file Excel dạng binary stream.\n" +
        "- 400 BadRequest: ID đợt hiến máu không hợp lệ.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadImportTemplate(int id)
    {
        var (content, fileName) = await _donorImportService.GenerateTemplateAsync(id);
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("{id:int}/donors/import")]
    [HasPermission("CAMPAIGN_EDIT")]
    [Consumes("multipart/form-data")]
    [EndpointSummary("Nhập danh sách người hiến máu từ file Excel")]
    [EndpointDescription(
        "Nhập danh sách người hiến máu từ file Excel theo mẫu của đợt hiến máu:\n" +
        "- Kiểm tra và tạo mới hoặc liên kết hồ sơ người hiến (Donor).\n" +
        "- Tự động tạo lịch hẹn (DonationAppointment) theo đúng khung giờ (Slot).\n" +
        "- Tự động lưu bảng hỏi khảo sát (PreScreeningSurvey) nếu có câu trả lời.\n" +
        "- Trả về báo cáo tổng kết gồm số lượng thành công và chi tiết các dòng bị lỗi (nếu có).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_EDIT** (BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Nhập file hoàn tất, trả về báo cáo kết quả chi tiết.\n" +
        "- 400 BadRequest: File không hợp lệ hoặc đợt hiến máu đã kết thúc / đã hủy.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(DonorImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportDonors(int id, IFormFile file)
    {
        var result = await _donorImportService.ImportDonorsAsync(id, file);
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
        "Chỉnh sửa thông tin chi tiết một đợt hiến máu đã có.\n\n" +
        "**Quy tắc nghiệp vụ:** Chỉ cho phép chỉnh sửa khi đợt hiến máu đang ở trạng thái Bản nháp (**DRAFT**) " +
        "hoặc Bị từ chối (**REJECTED**). Các trạng thái khác (PENDING_APPROVAL, APPROVED, IN_PROGRESS, COMPLETED, CANCELLED) " +
        "sẽ bị chặn để bảo đảm tính toàn vẹn dữ liệu.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_EDIT** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Cập nhật thành công, trả về chi tiết đợt hiến máu sau khi cập nhật.\n" +
        "- 400 BadRequest: Đợt hiến máu không ở trạng thái DRAFT/REJECTED hoặc dữ liệu ngày/chỉ tiêu không hợp lệ.\n" +
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

    [HttpPatch("{id:int}/submit-approval")]
    [HasPermission("CAMPAIGN_EDIT")]
    [EndpointSummary("Gửi đề xuất đợt hiến máu lên Lãnh đạo phê duyệt")]
    [EndpointDescription(
        "Nhân viên Ngân hàng máu (BBNK) chuyển trạng thái đợt hiến máu từ Bản nháp (**DRAFT**) " +
        "hoặc Bị từ chối (**REJECTED**) sang Chờ phê duyệt (**PENDING_APPROVAL**) để Lãnh đạo bệnh viện xem xét.\n\n" +
        "**Quy tắc nghiệp vụ:** Không thể gửi duyệt các đợt hiến máu có ngày kết thúc nằm trong quá khứ.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_EDIT** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Gửi duyệt thành công, trả về chi tiết đợt hiến máu với trạng thái PENDING_APPROVAL.\n" +
        "- 400 BadRequest: Đợt hiến máu không ở trạng thái DRAFT/REJECTED hoặc đã quá hạn.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitForApproval(int id)
    {
        var result = await _campaignsService.SubmitForApprovalAsync(id);
        return Ok(result);
    }

    [HttpPatch("{id:int}/approval")]
    [HasPermission("CAMPAIGN_APPROVE")]
    [EndpointSummary("Phê duyệt hoặc từ chối đề xuất đợt hiến máu")]
    [EndpointDescription(
        "Lãnh đạo bệnh viện (MGT) duyệt hoặc từ chối đề xuất đợt hiến máu đang ở trạng thái **PENDING_APPROVAL**:\n" +
        "- Nếu đồng ý (`isApproved: true`): Trạng thái chuyển sang **APPROVED**.\n" +
        "- Nếu từ chối (`isApproved: false`): Bắt buộc phải có lý do từ chối (`rejectionReason`), trạng thái chuyển sang **REJECTED**.\n\n" +
        "Thông tin người duyệt và thời điểm duyệt sẽ được tự động ghi nhận theo tài khoản đang đăng nhập.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_APPROVE** (vai trò MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Xét duyệt thành công, trả về chi tiết đợt hiến máu sau khi cập nhật.\n" +
        "- 400 BadRequest: Đợt hiến máu không ở trạng thái PENDING_APPROVAL hoặc thiếu lý do từ chối.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveCampaign(int id, [FromBody] ApproveCampaignRequest request)
    {
        var userId = User.GetUserId();
        var result = await _campaignsService.ApproveCampaignAsync(id, request, userId);
        return Ok(result);
    }

    [HttpPatch("{id:int}/cancel")]
    [HasPermission("CAMPAIGN_CANCEL")]
    [EndpointSummary("Hủy bỏ đợt hiến máu (Dành riêng cho Lãnh đạo)")]
    [EndpointDescription(
        "Lãnh đạo bệnh viện (MGT) hủy bỏ đợt hiến máu đang ở trạng thái Đã duyệt (**APPROVED**) hoặc Đang diễn ra (**IN_PROGRESS**):\n" +
        "- Bắt buộc phải có lý do hủy (`reason`, tối thiểu 5 ký tự, tối đa 255 ký tự).\n" +
        "- Trạng thái sẽ được chuyển sang **CANCELLED**.\n" +
        "- Người hủy (`cancelled_by`), thời điểm hủy (`cancelled_at`) và lý do hủy (`cancellation_reason`) được tự động lưu trữ kiểm toán.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_CANCEL** (vai trò MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Hủy đợt hiến máu thành công, trả về chi tiết đợt hiến máu sau khi hủy.\n" +
        "- 400 BadRequest: Đợt hiến máu không ở trạng thái APPROVED hoặc IN_PROGRESS, hoặc thiếu lý do hủy.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_CANCEL.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelCampaign(int id, [FromBody] CancelCampaignRequest request)
    {
        var userId = User.GetUserId();
        var result = await _campaignsService.CancelCampaignAsync(id, request, userId);
        return Ok(result);
    }

    [HttpPatch("{id:int}/status")]
    [HasPermission("CAMPAIGN_EDIT")]
    [EndpointSummary("Chuyển đổi trạng thái vận hành đợt hiến máu")]
    [EndpointDescription(
        "Quản lý vòng đời vận hành của đợt hiến máu theo quy trình nghiệp vụ:\n" +
        "- Chuyển sang **IN_PROGRESS**: Bắt đầu triển khai chiến dịch hiến máu (chỉ được chuyển từ **APPROVED**).\n" +
        "- Chuyển sang **COMPLETED**: Hoàn thành đợt hiến máu khi kết thúc ca hiến (chỉ được chuyển từ **IN_PROGRESS**).\n\n" +
        "**Lưu ý:** Thao tác hủy đợt hiến (**CANCELLED**) không thể thực hiện qua endpoint này mà bắt buộc phải thông qua endpoint hủy chuyên biệt của Lãnh đạo (`PATCH /api/campaigns/{id}/cancel`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_EDIT** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Chuyển đổi trạng thái vận hành thành công, trả về chi tiết đợt hiến máu sau khi cập nhật.\n" +
        "- 400 BadRequest: Trạng thái đích không hợp lệ hoặc bước chuyển trạng thái vi phạm quy trình nghiệp vụ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCampaignStatus(int id, [FromBody] UpdateCampaignStatusRequest request)
    {
        var userId = User.GetUserId();
        var result = await _campaignsService.UpdateCampaignStatusAsync(id, request, userId);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission("CAMPAIGN_EDIT")]
    [EndpointSummary("Xóa đợt hiến máu (Chỉ áp dụng cho Bản nháp)")]
    [EndpointDescription(
        "Xóa hoàn toàn đợt hiến máu khỏi cơ sở dữ liệu.\n\n" +
        "**Quy tắc nghiệp vụ:** Nghiêm ngặt chỉ áp dụng khi đợt hiến máu đang ở trạng thái Bản nháp (**DRAFT**) và chưa phát sinh bất kỳ lịch hẹn nào.\n\n" +
        "Đối với các đợt hiến máu đã được phê duyệt hoặc đang diễn ra, " +
        "vui lòng sử dụng API chuyển trạng thái sang **CANCELLED** để đảm bảo tính toàn vẹn dữ liệu và lịch sử kiểm toán.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **CAMPAIGN_EDIT** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 204 NoContent: Xóa đợt hiến máu thành công.\n" +
        "- 400 BadRequest: Đợt hiến máu không ở trạng thái DRAFT hoặc đã có lịch hẹn phát sinh.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền CAMPAIGN_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy đợt hiến máu với ID chỉ định.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCampaign(int id)
    {
        await _campaignsService.DeleteCampaignAsync(id);
        return NoContent();
    }
}
