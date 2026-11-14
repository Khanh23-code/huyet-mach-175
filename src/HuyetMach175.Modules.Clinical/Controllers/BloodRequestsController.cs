using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Clinical.Controllers;

[ApiController]
[Route("api/blood-requests")]
[Authorize]
[Tags("Clinical.BloodRequests")]
public class BloodRequestsController : ControllerBase
{
    private readonly IBloodRequestsService _bloodRequestsService;

    public BloodRequestsController(IBloodRequestsService bloodRequestsService)
    {
        _bloodRequestsService = bloodRequestsService;
    }

    [HttpPost]
    [HasPermission("BLOOD_REQUEST_EDIT")]
    [EndpointSummary("Tạo mới hoặc lưu nháp phiếu yêu cầu máu")]
    [EndpointDescription(
        "Bác sĩ điều trị tạo mới hoặc lưu nháp phiếu yêu cầu cấp máu cho người bệnh.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- `isDraft = true`: Tạo phiếu ở trạng thái `DRAFT`.\n" +
        "- `isDraft = false`: Tạo và gửi phiếu ngay ở trạng thái `PENDING`.\n" +
        "- Hệ thống tự động sinh mã phiếu duy nhất dạng `YC-YYMMDD-XXX`.\n" +
        "- Không được chọn trùng `componentTypeId` trong cùng một phiếu; `requestedUnits` phải lớn hơn 0.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Tạo phiếu yêu cầu thành công.\n" +
        "- 400 BadRequest: Dữ liệu đầu vào không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_EDIT.\n" +
        "- 404 NotFound: Loại chế phẩm máu không tồn tại.")]
    [ProducesResponseType(typeof(CreateBloodRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBloodRequest([FromBody] CreateBloodRequestDto request)
    {
        var doctorId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodRequestsService.CreateBloodRequestAsync(request, doctorId, ipAddress);
        return CreatedAtAction(nameof(GetBloodRequestById), new { id = result.RequestId }, result);
    }

    [HttpGet("clinical")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Lấy danh sách phiếu yêu cầu máu phía Lâm sàng")]
    [EndpointDescription(
        "Trả về danh sách các phiếu yêu cầu máu do bác sĩ đang đăng nhập tạo, có phân trang, tìm kiếm và lọc theo mức độ khẩn cấp, trạng thái, kèm `currentStep` cho Stepper UI.\n\n" +
        "**Quy ước `currentStep`:**\n" +
        "- `DRAFT`: `0` (Nháp)\n" +
        "- `PENDING`: `1` (Đã gửi)\n" +
        "- `READY`: `2` (Đã tiếp nhận đơn yêu cầu và đồng ý cấp phát)\n" +
        "- `COMPLETED`: `3` (Đã tiếp nhận máu và đóng đơn)\n" +
        "- `REJECTED`: `-1` (Từ chối)\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách phiếu yêu cầu phân trang.\n" +
        "- 400 BadRequest: Tham số lọc hoặc phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<ClinicalBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClinicalBloodRequests([FromQuery] ClinicalBloodRequestFilterDto filter)
    {
        var doctorId = User.GetUserId();
        var result = await _bloodRequestsService.GetClinicalBloodRequestsAsync(filter, doctorId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Xem chi tiết phiếu yêu cầu máu")]
    [EndpointDescription(
        "Trả về thông tin chi tiết một phiếu yêu cầu máu gồm thông tin Master, danh sách chế phẩm yêu cầu kèm tên chế phẩm và danh sách các túi máu đã được giữ chỗ / cấp phát (nếu có).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW** (vai trò CLIN chỉ xem được phiếu của mình; BBNK / MGT xem được toàn bộ).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết phiếu yêu cầu máu.\n" +
        "- 400 BadRequest: Mã phiếu không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập phiếu này.\n" +
        "- 404 NotFound: Không tìm thấy phiếu yêu cầu máu.")]
    [ProducesResponseType(typeof(BloodRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBloodRequestById(int id)
    {
        var currentUserId = User.GetUserId();
        var result = await _bloodRequestsService.GetBloodRequestByIdAsync(id, currentUserId);
        return Ok(result);
    }
}
