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

    [HttpPost("dispatch")]
    [HasPermission("BLOOD_REQUEST_APPROVE")]
    [EndpointSummary("Quét barcode túi máu xuất kho cấp phát")]
    [EndpointDescription(
        "Kỹ thuật viên Ngân hàng máu quét barcode túi máu đã được giữ chỗ (`RESERVED`) để xuất kho bàn giao cho Lâm sàng.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Kiểm tra túi máu tồn tại, chưa hết hạn và đang ở trạng thái `RESERVED`.\n" +
        "- Chuyển trạng thái túi máu sang `ISSUED`, giải phóng vị trí tủ bảo quản (`LocationId = null`).\n" +
        "- Cập nhật bản ghi `blood_allocations` sang `ISSUED`, lưu `issued_by` và `issued_at`.\n" +
        "- Ghi vết `audit_logs` và `blood_bag_status_logs` (`RESERVED` -> `ISSUED`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Xuất kho thành công, trả về thông tin túi máu và phiếu yêu cầu đối soát.\n" +
        "- 400 BadRequest: Mã vạch trống hoặc túi máu hết hạn / không ở trạng thái `RESERVED`.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy túi máu hoặc lượt giữ chỗ tương ứng.\n" +
        "- 409 Conflict: Túi máu đã được xuất kho trước đó.")]
    [ProducesResponseType(typeof(DispatchBloodBagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DispatchBag([FromBody] DispatchBloodBagRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodHandoversService.DispatchBagAsync(request, userId, ipAddress);
        return Ok(result);
    }

    [HttpGet("clinical/pending-receipt")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Lấy danh sách túi máu đang chờ nhận tại Lâm sàng")]
    [EndpointDescription(
        "Trả về các túi máu đã xuất kho (`ISSUED`), chưa được xác nhận nhận tại Lâm sàng (`received_by IS NULL`) thuộc các phiếu yêu cầu trong cùng khoa của người đang đăng nhập.\n\n" +
        "**Hỗ trợ lọc (trong phạm vi khoa hiện tại):**\n" +
        "- `doctorId`: Lọc theo ID bác sĩ lập phiếu.\n" +
        "- `doctorName`: Tìm kiếm gần đúng theo tên bác sĩ lập phiếu.\n" +
        "- `requestCode`: Tìm kiếm gần đúng theo mã phiếu yêu cầu.\n\n" +
        "**Quy tắc sắp xếp:** Ưu tiên đơn do chính người dùng tạo (`isOwnRequest = true`) lên đầu, sau đó đến mức độ khẩn cấp (`EMERGENCY` -> `URGENT` -> `ROUTINE`) và thời gian xuất kho.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách túi máu đang chờ nhận.\n" +
        "- 400 BadRequest: Tham số lọc `doctorId` không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_VIEW.")]
    [ProducesResponseType(typeof(List<PendingReceiptBagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingClinicalReceipts([FromQuery] PendingClinicalReceiptFilterDto filter)
    {
        var userId = User.GetUserId();
        var result = await _bloodHandoversService.GetPendingClinicalReceiptsAsync(filter, userId);
        return Ok(result);
    }

    [HttpPost("clinical/confirm-receipt")]
    [HasPermission("BLOOD_REQUEST_EDIT")]
    [EndpointSummary("Quét đối soát & xác nhận thực nhận túi máu tại Lâm sàng")]
    [EndpointDescription(
        "Điều dưỡng / bác sĩ thuộc cùng khoa quét barcode túi máu để xác nhận thực nhận túi máu từ Ngân hàng máu; nếu nhận hộ phiếu của bác sĩ khác thì bắt buộc đối chiếu thêm mã người bệnh (`expectedPatientCode`).\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Chỉ cho phép người dùng thuộc cùng khoa với bác sĩ tạo phiếu yêu cầu thực hiện xác nhận.\n" +
        "- Luôn đối chiếu `scannedBarcode` khớp với mã túi máu; nếu `DoctorId != CurrentUserId` (nhận hộ trong khoa), bắt buộc `expectedPatientCode` khớp với mã người bệnh trên phiếu.\n" +
        "- Cập nhật `blood_allocations`: `received_by = CurrentUserId`, `status = COMPLETED`.\n" +
        "- Nếu toàn bộ chế phẩm trên phiếu yêu cầu đã được giữ chỗ đủ và tất cả lượt phân bổ đã `COMPLETED`, tự động chuyển trạng thái phiếu `blood_requests` sang `COMPLETED`.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Đối soát và xác nhận nhận túi máu thành công.\n" +
        "- 400 BadRequest: Barcode hoặc mã người bệnh đối soát không khớp / trạng thái túi không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_EDIT hoặc khác khoa điều trị.\n" +
        "- 404 NotFound: Không tìm thấy lượt phân bổ.\n" +
        "- 409 Conflict: Túi máu này đã được xác nhận nhận trước đó.")]
    [ProducesResponseType(typeof(ConfirmReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmClinicalReceipt([FromBody] ConfirmReceiptRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodHandoversService.ConfirmClinicalReceiptAsync(request, userId, ipAddress);
        return Ok(result);
    }
}
