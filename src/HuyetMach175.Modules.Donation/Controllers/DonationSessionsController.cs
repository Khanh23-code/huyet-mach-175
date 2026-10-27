using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Sessions;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.Controllers;

[ApiController]
[Route("api/donation-sessions")]
[Authorize]
[Tags("Donation.Sessions")]
public class DonationSessionsController : ControllerBase
{
    private readonly IDonationSessionsService _sessionsService;

    public DonationSessionsController(IDonationSessionsService sessionsService)
    {
        _sessionsService = sessionsService;
    }

    [HttpGet]
    [HasPermission("SESSION_VIEW")]
    [EndpointSummary("Lấy danh sách hàng chờ và lịch sử phiên lấy máu")]
    [EndpointDescription(
        "Lấy danh sách hàng chờ lấy máu (`WAITING_COLLECTION`) hoặc các ca đã hoàn tất/hủy trong ngày phục vụ Kỹ thuật viên (KTV) tại phòng thu gom.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Nếu không truyền `date`, hệ thống mặc định lấy ngày hiện tại.\n" +
        "- Lọc theo trạng thái phiên (`status`): `WAITING_COLLECTION`, `COMPLETED`, `ABORTED`.\n" +
        "- Danh sách được sắp xếp theo thứ tự Bác sĩ hoàn thành khám sàng lọc (`screenedAt`) tăng dần (FIFO).\n" +
        "- Hỗ trợ tìm kiếm nhanh (`search`) theo họ tên, CCCD, số điện thoại hoặc mã QR.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **SESSION_VIEW** (các vai trò như DON, BBNK, MGT, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách phiên lấy máu phân trang.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập SESSION_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<SessionQueueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessions([FromQuery] SessionQueueFilterDto filter)
    {
        var result = await _sessionsService.GetSessionsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission("SESSION_VIEW")]
    [EndpointSummary("Xem chi tiết thông tin phiên lấy máu")]
    [EndpointDescription(
        "Lấy toàn bộ thông tin chi tiết của 1 phiên lấy máu để KTV đối soát thông tin người hiến và chỉ định của Bác sĩ trước khi cắm kim.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Trả về thông tin người hiến (`donor`): Mã người hiến, họ tên, CCCD, giới tính, nhóm máu (nếu có).\n" +
        "- Trả về tóm tắt khám sàng lọc (`screeningSummary`): Cân nặng, huyết áp, mạch, hemoglobin, ghi chú lâm sàng, bác sĩ chỉ định và giờ khám.\n" +
        "- Nếu phiên đã hoàn tất hoặc có sự cố, trả về thông tin KTV thực hiện (`phlebotomistName`), giờ hoàn thành và mã túi máu (`bloodBagBarcode`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **SESSION_VIEW**.\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết phiên lấy máu.\n" +
        "- 400 BadRequest: ID phiên lấy máu không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập SESSION_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy phiên lấy máu với ID chỉ định.")]
    [ProducesResponseType(typeof(SessionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionById([FromRoute] int id)
    {
        var result = await _sessionsService.GetSessionByIdAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:int}/collection")]
    [HasPermission("SESSION_EDIT")]
    [EndpointSummary("Cập nhật kết quả thu gom máu và đóng phiên")]
    [EndpointDescription(
        "KTV cập nhật thể tích máu thực tế thu được, ghi nhận sự cố (nếu có) và đóng phiên hiến máu.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Trích xuất phlebotomist_id từ JWT Token và cập nhật completedAt = CURRENT_TIMESTAMP.\n" +
        "- Nếu status == COMPLETED: Bắt buộc nhập thể tích máu thực tế (actualVolumeMl). Bảng donors tự động tăng total_donations += 1 và cập nhật last_donation_date. Đồng thời tự động tạo một túi máu mới trong bảng blood_bags với trạng thái NEWLY_COLLECTED phục vụ chuyển sang Module Kho & Xét nghiệm (W2).\n" +
        "- Nếu status == ABORTED: Bắt buộc ghi nhận lý do sự cố (collectionIncident), không tăng số lần hiến và không tạo túi máu.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **SESSION_EDIT**.\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Đóng phiên lấy máu thành công.\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ hoặc phiên đã đóng trước đó.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập SESSION_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy phiên lấy máu.")]
    [ProducesResponseType(typeof(CollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteCollection(
        [FromRoute] int id,
        [FromBody] UpdateCollectionRequest request)
    {
        var phlebotomistId = User.GetUserId();
        var result = await _sessionsService.CompleteCollectionAsync(id, request, phlebotomistId);
        return Ok(result);
    }
}