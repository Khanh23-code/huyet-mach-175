using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;
using HuyetMach175.Modules.Clinical.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Clinical.Controllers;

[ApiController]
[Route("api/blood-handovers")]
[Authorize]
[Tags("Clinical.BloodHandovers")]
public class BloodHandoversController : ControllerBase
{
    private readonly IBloodHandoversService _bloodHandoversService;

    public BloodHandoversController(IBloodHandoversService bloodHandoversService)
    {
        _bloodHandoversService = bloodHandoversService;
    }

    [HttpGet("reserved-requests")]
    [HasPermission("BLOOD_REQUEST_APPROVE")]
    [EndpointSummary("Lấy danh sách đơn yêu cầu kèm các túi máu đang giữ chỗ (RESERVED) chờ bàn giao")]
    [EndpointDescription(
        "Nhóm toàn bộ túi máu đang ở trạng thái `RESERVED` theo từng phiếu yêu cầu (`BloodRequest`), kèm thông tin bệnh nhân, khoa lâm sàng và trạng thái phiên bàn giao đang chờ (nếu có) để BBNK mở phiên bàn giao theo lô đúng từng đơn.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_APPROVE** (vai trò BBNK).")]
    [ProducesResponseType(typeof(List<ReservedHandoverRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReservedRequests([FromQuery] string? search, [FromQuery] string? urgencyLevel)
    {
        var result = await _bloodHandoversService.GetReservedRequestsForHandoverAsync(search, urgencyLevel);
        return Ok(result);
    }

    [HttpPost("sessions")]
    [HasPermission("BLOOD_REQUEST_APPROVE")]
    [EndpointSummary("Khởi tạo phiên bàn giao máu 2 pha theo lô (Pha 1 - BBNK)")]
    [EndpointDescription(
        "Kỹ thuật viên Ngân hàng máu quét một hoặc nhiều barcode túi máu đang giữ chỗ (`RESERVED`) của cùng một phiếu yêu cầu để mở phiên bàn giao đồng thời tại quầy.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Toàn bộ túi phải đang `RESERVED`, chưa hết hạn và chưa nằm trong phiên `PENDING` khác.\n" +
        "- Trong thời gian chờ xác nhận (`timeoutMinutes` từ 3–5 phút, mặc định 5 phút), túi máu vẫn giữ nguyên trạng thái `RESERVED` và vị trí tủ bảo quản.\n" +
        "- Tự động gửi thông báo in-app tới khoa Lâm sàng lập phiếu để quét xác nhận Pha 2.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Khởi tạo phiên bàn giao thành công.\n" +
        "- 400 BadRequest: Danh sách barcode không hợp lệ, túi không ở `RESERVED`, hết hạn hoặc lẫn nhiều phiếu yêu cầu.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy túi máu hoặc lượt giữ chỗ.\n" +
        "- 409 Conflict: Một hoặc nhiều túi đang nằm trong phiên bàn giao `PENDING` khác.")]
    [ProducesResponseType(typeof(HandoverSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHandoverSession([FromBody] CreateHandoverSessionRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodHandoversService.CreateHandoverSessionAsync(request, userId, ipAddress);
        return CreatedAtAction(nameof(GetHandoverSessionById), new { sessionId = result.HandoverSessionId }, result);
    }

    [HttpGet("sessions/pending")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Lấy danh sách phiên bàn giao đang chờ xác nhận tại quầy")]
    [EndpointDescription(
        "Trả về các phiên bàn giao đang ở trạng thái `PENDING` (chưa quá hạn), kèm danh sách túi máu trong lô và số giây còn lại (`remainingSeconds`).\n\n" +
        "**Phân quyền phạm vi dữ liệu:**\n" +
        "- Vai trò `BBNK`, `MGT`, `SYS`: Xem toàn bộ phiên chờ xác nhận.\n" +
        "- Vai trò `CLIN`: Chỉ xem các phiên thuộc khoa của mình (ưu tiên đơn của chính bác sĩ đang đăng nhập lên đầu).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW**.\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách phiên bàn giao chờ xác nhận.\n" +
        "- 400 BadRequest: Tham số `doctorId` không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_VIEW.")]
    [ProducesResponseType(typeof(List<HandoverSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingHandoverSessions([FromQuery] PendingHandoverSessionFilterDto filter)
    {
        var userId = User.GetUserId();
        var result = await _bloodHandoversService.GetPendingHandoverSessionsAsync(filter, userId);
        return Ok(result);
    }

    [HttpGet("sessions/{sessionId:guid}")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Xem chi tiết phiên bàn giao máu")]
    [EndpointDescription(
        "Trả về trạng thái hiện tại, thời gian còn lại và danh sách túi máu của một phiên bàn giao.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW** (CLIN chỉ xem được phiên thuộc khoa của mình).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết phiên bàn giao.\n" +
        "- 400 BadRequest: `sessionId` không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Khác khoa điều trị hoặc không có quyền.\n" +
        "- 404 NotFound: Không tìm thấy phiên bàn giao.")]
    [ProducesResponseType(typeof(HandoverSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHandoverSessionById(Guid sessionId)
    {
        var userId = User.GetUserId();
        var result = await _bloodHandoversService.GetHandoverSessionByIdAsync(sessionId, userId);
        return Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/confirm")]
    [HasPermission("BLOOD_REQUEST_EDIT")]
    [EndpointSummary("Quét đối soát & xác nhận nhận lô túi máu tại quầy (Pha 2 - CLIN)")]
    [EndpointDescription(
        "Nhân sự Lâm sàng quét xác nhận đủ 100% barcode túi máu trong phiên bàn giao đang `PENDING` (chưa hết hạn).\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Chỉ người dùng thuộc cùng khoa với bác sĩ lập phiếu mới được xác nhận; nếu nhận hộ phiếu của bác sĩ khác (`DoctorId != CurrentUserId`), bắt buộc truyền `expectedPatientCode` khớp mã người bệnh.\n" +
        "- Chuyển phiên sang `CONFIRMED`, đồng loạt chuyển các túi trong phiên từ `RESERVED` sang `ISSUED`, giải phóng vị trí tủ (`LocationId = null`), cập nhật `blood_allocations` sang `COMPLETED` (`issued_by`, `issued_at`, `received_by`).\n" +
        "- Nếu toàn bộ chế phẩm của phiếu đã giữ chỗ đủ và mọi lượt phân bổ đã hoàn tất, tự động chốt `blood_requests.status = COMPLETED`.\n" +
        "- Ghi vết đầy đủ `audit_logs` và `blood_bag_status_logs` (`RESERVED` -> `ISSUED`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Bàn giao thành công.\n" +
        "- 400 BadRequest: Phiên đã hết hạn/đã hủy, danh sách barcode quét không khớp đủ, hoặc thiếu/sai `expectedPatientCode` khi nhận hộ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_EDIT hoặc khác khoa điều trị.\n" +
        "- 404 NotFound: Không tìm thấy phiên bàn giao.\n" +
        "- 409 Conflict: Phiên bàn giao đã được xác nhận trước đó.")]
    [ProducesResponseType(typeof(HandoverSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmHandoverSession(Guid sessionId, [FromBody] ConfirmHandoverSessionRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodHandoversService.ConfirmHandoverSessionAsync(sessionId, request, userId, ipAddress);
        return Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/cancel")]
    [HasPermission("BLOOD_REQUEST_APPROVE")]
    [EndpointSummary("Hủy sớm phiên bàn giao tại quầy (BBNK)")]
    [EndpointDescription(
        "Kỹ thuật viên Ngân hàng máu chủ động hủy phiên bàn giao đang `PENDING` khi có sự cố tại quầy.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Chuyển trạng thái phiên sang `CANCELLED`.\n" +
        "- Toàn bộ túi máu giữ nguyên trạng thái `RESERVED`, giữ nguyên `blood_allocations`, vị trí tủ và trạng thái phiếu `blood_requests`.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Hủy phiên thành công.\n" +
        "- 400 BadRequest: Phiên không ở trạng thái `PENDING` hoặc lý do vượt quá 255 ký tự.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy phiên bàn giao.\n" +
        "- 409 Conflict: Phiên đã xác nhận hoàn tất trước đó.")]
    [ProducesResponseType(typeof(HandoverSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelHandoverSession(Guid sessionId, [FromBody] CancelHandoverSessionRequest? request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodHandoversService.CancelHandoverSessionAsync(sessionId, request, userId, ipAddress);
        return Ok(result);
    }

    [HttpGet("clinical/transfusion-scan/{barcode}")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Quét barcode túi máu đối chiếu thông tin trước khi truyền tại giường")]
    [EndpointDescription(
        "Bác sĩ/Điều dưỡng Lâm sàng quét barcode túi máu đang ở trạng thái `ISSUED` ngay trước khi mở túi cắm dây truyền để kiểm tra đối chiếu thông tin người bệnh.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ cho phép nhân sự thuộc cùng khoa với phiếu yêu cầu thực hiện quét.\n" +
        "- Hiển thị thông tin đối chiếu bắt buộc: Tên bệnh nhân, Mã bệnh nhân, Nhóm máu người bệnh, Nhóm máu túi, Loại chế phẩm, Hạn dùng.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về thông tin đối chiếu truyền máu.\n" +
        "- 400 BadRequest: Mã vạch trống hoặc túi không ở trạng thái `ISSUED`.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_VIEW hoặc khác khoa điều trị.\n" +
        "- 404 NotFound: Không tìm thấy túi máu hoặc lượt cấp phát.\n" +
        "- 409 Conflict: Túi máu đã được xác nhận truyền trước đó (`TRANSFUSED`).")]
    [ProducesResponseType(typeof(TransfusionScanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ScanBloodBagForTransfusion(string barcode)
    {
        var userId = User.GetUserId();
        var result = await _bloodHandoversService.ScanBloodBagForTransfusionAsync(barcode, userId);
        return Ok(result);
    }

    [HttpPost("clinical/confirm-transfusion")]
    [HasPermission("BLOOD_REQUEST_EDIT")]
    [EndpointSummary("Xác nhận bắt đầu truyền máu thực tế tại giường (`ISSUED` -> `TRANSFUSED`)")]
    [EndpointDescription(
        "Lâm sàng xác nhận mở túi và bắt đầu truyền máu cho người bệnh, khóa vĩnh viễn khả năng hoàn trả túi máu.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Chỉ cho phép nhân sự thuộc cùng khoa với phiếu yêu cầu xác nhận.\n" +
        "- Bắt buộc truyền `expectedPatientCode` khớp với mã người bệnh trên phiếu để đối chiếu an toàn tại giường.\n" +
        "- Túi phải đang ở trạng thái `ISSUED` và chưa hết hạn sử dụng (`ExpiredAt > now`).\n" +
        "- Chuyển trạng thái túi máu sang `TRANSFUSED` (trạng thái kết thúc vĩnh viễn) và ghi vết kép `audit_logs`, `blood_bag_status_logs` (`ISSUED` -> `TRANSFUSED`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Xác nhận truyền máu thành công.\n" +
        "- 400 BadRequest: Túi hết hạn, không ở `ISSUED`, hoặc lệch thông tin đối soát.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_EDIT hoặc khác khoa điều trị.\n" +
        "- 404 NotFound: Không tìm thấy túi máu hoặc lượt cấp phát.\n" +
        "- 409 Conflict: Túi đã `TRANSFUSED` hoặc đang có phiếu hoàn trả chờ kiểm định.")]
    [ProducesResponseType(typeof(ConfirmTransfusionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmTransfusion([FromBody] ConfirmTransfusionRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodHandoversService.ConfirmTransfusionAsync(request, userId, ipAddress);
        return Ok(result);
    }
}
