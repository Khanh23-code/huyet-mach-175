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

    [HttpPut("{id:int}")]
    [HasPermission("BLOOD_REQUEST_EDIT")]
    [EndpointSummary("Cập nhật nội dung phiếu yêu cầu máu đang ở trạng thái DRAFT")]
    [EndpointDescription(
        "Bác sĩ điều trị chỉnh sửa nội dung phiếu yêu cầu máu đang ở trạng thái Bản nháp (`DRAFT`) theo đúng ID phiếu gốc, không tạo bản ghi mới.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ chủ sở hữu phiếu (`DoctorId == CurrentUserId`) mới được chỉnh sửa.\n" +
        "- Chỉ cho phép cập nhật khi phiếu đang ở trạng thái `DRAFT`; nếu phiếu đã gửi (`PENDING`, `READY`, `COMPLETED`, `REJECTED`) sẽ trả `409 Conflict`.\n" +
        "- Giữ nguyên `requestId`, `requestCode`, `status = DRAFT`.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Cập nhật thành công, trả về chi tiết phiếu sau khi cập nhật.\n" +
        "- 400 BadRequest: Dữ liệu đầu vào không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_EDIT hoặc không phải bác sĩ tạo phiếu.\n" +
        "- 404 NotFound: Không tìm thấy phiếu yêu cầu hoặc loại chế phẩm máu.\n" +
        "- 409 Conflict: Phiếu không còn ở trạng thái DRAFT.")]
    [ProducesResponseType(typeof(BloodRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraftBloodRequest(int id, [FromBody] UpdateBloodRequestDto request)
    {
        var doctorId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodRequestsService.UpdateDraftBloodRequestAsync(id, request, doctorId, ipAddress);
        return Ok(result);
    }

    [HttpPatch("{id:int}/submit")]
    [HasPermission("BLOOD_REQUEST_EDIT")]
    [EndpointSummary("Gửi chính thức phiếu yêu cầu máu đang ở trạng thái DRAFT")]
    [EndpointDescription(
        "Chuyển trạng thái phiếu yêu cầu máu gốc từ `DRAFT` sang `PENDING` theo `id` mà không tạo bản ghi mới.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ chủ sở hữu phiếu (`DoctorId == CurrentUserId`) mới được gửi.\n" +
        "- Chỉ xử lý khi phiếu đang ở trạng thái `DRAFT`; cập nhật nguyên tử để chống gửi lặp hoặc gửi đồng thời (`409 Conflict`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Gửi phiếu thành công, phiếu chuyển sang trạng thái `PENDING`.\n" +
        "- 400 BadRequest: ID không hợp lệ hoặc phiếu nháp thiếu chế phẩm hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_EDIT hoặc không phải bác sĩ tạo phiếu.\n" +
        "- 404 NotFound: Không tìm thấy phiếu yêu cầu máu.\n" +
        "- 409 Conflict: Phiếu đã được gửi trước đó hoặc không còn ở trạng thái DRAFT.")]
    [ProducesResponseType(typeof(SubmitBloodRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitDraftBloodRequest(int id)
    {
        var doctorId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodRequestsService.SubmitDraftBloodRequestAsync(id, doctorId, ipAddress);
        return Ok(result);
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

    [HttpPut("{id:int}/reject")]
    [HasPermission("BLOOD_REQUEST_APPROVE")]
    [EndpointSummary("Từ chối phiếu yêu cầu máu")]
    [EndpointDescription(
        "Ngân hàng máu từ chối phiếu yêu cầu đang ở trạng thái `PENDING` kèm lý do cụ thể.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Nếu phiếu đã giữ chỗ dở một phần trước đó, hệ thống tự động giải phóng các túi đang `RESERVED` về lại `AVAILABLE`, gỡ khóa giữ chỗ và reset `allocated_units = 0`.\n" +
        "- Chuyển trạng thái phiếu sang `REJECTED`, lưu `rejection_reason` và ghi vết `audit_logs`, `blood_bag_status_logs`.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Từ chối phiếu thành công.\n" +
        "- 400 BadRequest: Thiếu lý do hoặc trạng thái phiếu không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy phiếu yêu cầu máu.")]
    [ProducesResponseType(typeof(RejectBloodRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectBloodRequest(int id, [FromBody] RejectBloodRequestDto request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodRequestsService.RejectBloodRequestAsync(id, request, userId, ipAddress);
        return Ok(result);
    }
}
