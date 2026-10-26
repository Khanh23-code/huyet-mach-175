using HuyetMach175.Modules.Donation.DTOs.Appointments;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
[Tags("Donation.Appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _appointmentsService;
    private readonly ICheckInService _checkInService;

    public AppointmentsController(
        IAppointmentsService appointmentsService,
        ICheckInService checkInService)
    {
        _appointmentsService = appointmentsService;
        _checkInService = checkInService;
    }

    [HttpGet]
    [HasPermission("APPOINTMENTS_VIEW")]
    [EndpointSummary("Lấy danh sách lịch hẹn hiến máu")]
    [EndpointDescription(
        "Lấy danh sách lịch hẹn hiến máu có hỗ trợ lọc theo ngày (date), chiến dịch (campaignId), " +
        "trạng thái (status: BOOKED, CHECKED_IN, CANCELLED, NO_SHOW), khung giờ (timeSlot), loại tiếp nhận (isWalkIn) " +
        "và tìm kiếm nhanh theo họ tên, CCCD hoặc số điện thoại của người hiến.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **APPOINTMENTS_VIEW** (các vai trò như DON, CLIN, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách lịch hẹn theo cấu trúc phân trang PagedResult.\n" +
        "- 400 BadRequest: Tham số phân trang hoặc bộ lọc không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập APPOINTMENTS_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<AppointmentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAppointments([FromQuery] AppointmentFilterDto filter)
    {
        var result = await _appointmentsService.GetAppointmentsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission("APPOINTMENTS_VIEW")]
    [EndpointSummary("Lấy thông tin chi tiết một lịch hẹn")]
    [EndpointDescription(
        "Trả về thông tin chi tiết của một lịch hẹn bao gồm dữ liệu người hiến, chiến dịch, trạng thái check-in " +
        "và kết quả phiếu khảo sát sàng lọc trước (nếu có).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **APPOINTMENTS_VIEW** (các vai trò như DON, CLIN, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết lịch hẹn.\n" +
        "- 400 BadRequest: ID lịch hẹn không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập APPOINTMENTS_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy lịch hẹn.")]
    [ProducesResponseType(typeof(AppointmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        var result = await _appointmentsService.GetAppointmentByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission("APPOINTMENTS_EDIT")]
    [EndpointSummary("Tạo lịch hẹn hiến máu mới (đặt trước)")]
    [EndpointDescription(
        "Tạo lịch hẹn hiến máu mới khi người hiến đăng ký qua ứng dụng hoặc do Lễ tân đặt lịch hộ.\n\n" +
        "**Quy tắc nghiệp vụ & Kiểm tra:**\n" +
        "- Người hiến phải tồn tại trong hệ thống.\n" +
        "- Ngày hẹn không được nằm trong quá khứ.\n" +
        "- Chiến dịch (nếu có) phải ở trạng thái APPROVED hoặc IN_PROGRESS, ngày hẹn phải thuộc thời gian diễn ra chiến dịch.\n" +
        "- Khoảng cách so với lần hiến máu gần nhất phải đủ tối thiểu 84 ngày tính đến ngày hẹn.\n" +
        "- Người hiến không được có lịch hẹn nào khác đang ở trạng thái BOOKED trong cùng ngày.\n" +
        "- Backend tự động sinh mã token QR bảo mật độc nhất (`HM175-{guid}`) và khởi tạo trạng thái `BOOKED`.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **APPOINTMENTS_EDIT** (các vai trò như DON, REC, CLIN, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Tạo lịch hẹn thành công, trả về thông tin chi tiết kèm mã token QR.\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ, không đủ khoảng cách 84 ngày hoặc trùng lịch hẹn trong ngày.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập APPOINTMENTS_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy người hiến hoặc chiến dịch.")]
    [ProducesResponseType(typeof(AppointmentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        var result = await _appointmentsService.CreateAppointmentAsync(request);
        return CreatedAtAction(nameof(GetAppointmentById), new { id = result.AppointmentId }, result);
    }

    [HttpPost("walk-in")]
    [HasPermission("APPOINTMENTS_EDIT")]
    [EndpointSummary("Tiếp nhận người hiến máu vãng lai tại bàn tiếp tân")]
    [EndpointDescription(
        "Tạo lịch hẹn tiếp nhận trực tiếp cho người hiến máu vãng lai tại bàn tiếp đón. " +
        "Lịch hẹn sẽ được gắn cờ `isWalkIn = true` và tự động chuyển ngay sang trạng thái `CHECKED_IN` với thời điểm tiếp nhận hiện tại.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Người hiến phải đủ điều kiện khoảng cách hiến máu tối thiểu 84 ngày tính đến hôm nay.\n" +
        "- Nếu người hiến đã có lịch hẹn đặt trước trong ngày, hệ thống sẽ từ chối để yêu cầu lễ tân thực hiện Check-in theo lịch đã có.\n" +
        "- Nếu người hiến đã được check-in trong ngày hôm nay, hệ thống sẽ cảnh báo không tiếp nhận trùng lặp.\n" +
        "- Tự động sinh token QR để in mã vạch/barcode dán hồ sơ giấy nếu cần.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **APPOINTMENTS_EDIT** (các vai trò như DON, REC, CLIN, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Tiếp nhận thành công, trạng thái CHECKED_IN.\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ, chưa đủ 84 ngày hoặc đã có lịch hẹn / check-in hôm nay.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập APPOINTMENTS_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy người hiến hoặc chiến dịch.")]
    [ProducesResponseType(typeof(AppointmentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateWalkInAppointment([FromBody] CreateWalkInAppointmentRequest request)
    {
        var result = await _checkInService.CreateWalkInAsync(request);
        return CreatedAtAction(nameof(GetAppointmentById), new { id = result.AppointmentId }, result);
    }

    [HttpPost("check-in")]
    [HasPermission("APPOINTMENTS_EDIT")]
    [EndpointSummary("Quét mã QR để Check-in tiếp đón người hiến máu")]
    [EndpointDescription(
        "Tiếp đón người hiến máu tại quầy lễ tân bằng cách quét mã token QR từ ứng dụng của người hiến.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Tìm kiếm lịch hẹn tương ứng với `qrCodeToken`.\n" +
        "- Kiểm tra trạng thái: chỉ cho phép check-in nếu lịch hẹn đang ở trạng thái `BOOKED`.\n" +
        "- Từ chối nếu lịch hẹn đã được check-in trước đó (chống quét lặp), đã bị hủy hoặc đánh dấu vắng mặt (NO_SHOW).\n" +
        "- Cập nhật `status = CHECKED_IN` và lưu thời điểm tiếp đón `checkedInAt = CURRENT_TIMESTAMP`.\n" +
        "- Trả về đầy đủ thông tin người hiến và kết quả khảo sát sàng lọc trước (nếu có) để bàn tiếp tân theo dõi.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **APPOINTMENTS_EDIT** (các vai trò như DON, REC, CLIN, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Check-in thành công.\n" +
        "- 400 BadRequest: Lịch hẹn đã check-in rồi, đã bị hủy hoặc trạng thái không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập APPOINTMENTS_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy lịch hẹn với mã QR được cung cấp.")]
    [ProducesResponseType(typeof(AppointmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckInAppointment([FromBody] CheckInAppointmentRequest request)
    {
        var result = await _checkInService.CheckInAsync(request);
        return Ok(result);
    }

    [HttpPatch("{id:int}/cancel")]
    [HasPermission("APPOINTMENTS_CANCEL")]
    [EndpointSummary("Hủy lịch hẹn hiến máu")]
    [EndpointDescription(
        "Hủy lịch hẹn hiến máu khi người hiến báo bận hoặc vắng mặt không đến (NO_SHOW).\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ cho phép hủy khi lịch hẹn đang ở trạng thái `BOOKED`.\n" +
        "- Nếu lịch hẹn đã `CHECKED_IN`, hệ thống sẽ từ chối không cho phép hủy.\n" +
        "- Cho phép chỉ định trạng thái là `CANCELLED` hoặc `NO_SHOW` (mặc định là `CANCELLED`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **APPOINTMENTS_CANCEL** (các vai trò như DON, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Hủy lịch hẹn thành công.\n" +
        "- 400 BadRequest: Lịch hẹn đã check-in hoặc đã hủy trước đó.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập APPOINTMENTS_CANCEL.\n" +
        "- 404 NotFound: Không tìm thấy lịch hẹn.")]
    [ProducesResponseType(typeof(AppointmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAppointment(int id, [FromBody] CancelAppointmentRequest? request)
    {
        var result = await _appointmentsService.CancelAppointmentAsync(id, request);
        return Ok(result);
    }
}
