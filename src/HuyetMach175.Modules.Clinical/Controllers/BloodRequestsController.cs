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

    [HttpGet("supply-demand")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Thống kê Bảng điều phối Cung - Cầu theo loại chế phẩm")]
    [EndpointDescription(
        "So sánh lượng túi máu khả dụng (`AVAILABLE` và chưa hết hạn) trong kho với tổng nhu cầu đang chờ cấp phát (`PENDING`) theo từng nhóm máu `ABO/Rh` cho một loại chế phẩm.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW** (vai trò BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về số liệu cung - cầu của cả 8 nhóm máu và danh sách nhóm máu thiếu hụt (`criticalShortages`).\n" +
        "- 400 BadRequest: `componentTypeId` không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_VIEW.\n" +
        "- 404 NotFound: Loại chế phẩm máu không tồn tại.")]
    [ProducesResponseType(typeof(SupplyDemandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplyDemand([FromQuery] int componentTypeId)
    {
        var result = await _bloodRequestsService.GetSupplyDemandAsync(componentTypeId);
        return Ok(result);
    }

    [HttpGet("queue")]
    [HasPermission("BLOOD_REQUEST_VIEW")]
    [EndpointSummary("Danh sách hàng đợi yêu cầu máu chờ thẩm định & cấp phát")]
    [EndpointDescription(
        "Trả về danh sách các phiếu yêu cầu đang ở trạng thái `PENDING`, sắp xếp ưu tiên theo mức độ khẩn cấp (`EMERGENCY` -> `URGENT` -> `ROUTINE`) và thời gian tạo tăng dần (`CreatedAt ASC`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_VIEW** (vai trò BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách hàng đợi phân trang.\n" +
        "- 400 BadRequest: Tham số lọc hoặc phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<CoordinationQueueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCoordinationQueue([FromQuery] BloodCoordinationQueueFilterDto filter)
    {
        var result = await _bloodRequestsService.GetCoordinationQueueAsync(filter);
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

    [HttpGet("{requestId:int}/items/{requestItemId:int}/available-bags")]
    [HasPermission("BLOOD_REQUEST_APPROVE")]
    [EndpointSummary("Lấy danh sách túi máu khả dụng để giữ chỗ cho dòng y lệnh")]
    [EndpointDescription(
        "Truy vấn các túi máu khả dụng (`AVAILABLE`, chưa hết hạn, chưa bị giữ chỗ) khớp loại chế phẩm của dòng y lệnh và nhóm máu người bệnh.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- `includeCompatible = false` (mặc định): Chỉ lấy túi trùng khớp chính xác `ABO/Rh`.\n" +
        "- `includeCompatible = true`: Bao gồm cả các túi nhóm máu hòa hợp an toàn theo loại chế phẩm (`WB/PRBC` theo quy tắc hồng cầu, `FFP` theo quy tắc huyết tương, `PLT` theo quy tắc tiểu cầu), luôn ưu tiên hiển thị túi trùng nhóm tuyệt đối lên đầu rồi đến hạn dùng gần nhất (`FEFO`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách túi máu khả dụng.\n" +
        "- 400 BadRequest: Tham số hoặc trạng thái phiếu không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy phiếu hoặc dòng y lệnh.")]
    [ProducesResponseType(typeof(List<AvailableBloodBagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableBagsForItem(int requestId, int requestItemId, [FromQuery] bool includeCompatible = false)
    {
        var result = await _bloodRequestsService.GetAvailableBagsForItemAsync(requestId, requestItemId, includeCompatible);
        return Ok(result);
    }

    [HttpPost("{requestId:int}/items/{requestItemId:int}/allocate")]
    [HasPermission("BLOOD_REQUEST_APPROVE")]
    [EndpointSummary("Xác nhận giữ chỗ túi máu cho dòng y lệnh")]
    [EndpointDescription(
        "Ngân hàng máu xác nhận giữ chỗ các túi máu đã chọn cho một dòng chế phẩm trong phiếu yêu cầu.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Kiểm tra số túi chọn không vượt quá phần còn thiếu (`requested_units - allocated_units`).\n" +
        "- Kiểm tra các túi đều ở trạng thái `AVAILABLE`, chưa hết hạn, đúng loại chế phẩm và tương thích miễn dịch `ABO/Rh`.\n" +
        "- Chuyển trạng thái túi máu sang `RESERVED`, tạo bản ghi `blood_allocations` (`RESERVED`), tăng `allocated_units`.\n" +
        "- Nếu toàn bộ dòng y lệnh trong phiếu đã đủ số lượng yêu cầu, chuyển trạng thái phiếu sang `READY`.\n" +
        "- Ghi vết đầy đủ vào `audit_logs` và `blood_bag_status_logs`.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_REQUEST_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Giữ chỗ thành công, trả về kết quả giữ chỗ và trạng thái mới của phiếu.\n" +
        "- 400 BadRequest: Dữ liệu hoặc trạng thái túi/phiếu không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_REQUEST_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy phiếu, dòng y lệnh hoặc túi máu.\n" +
        "- 409 Conflict: Một hoặc nhiều túi máu đã được giữ chỗ/cấp phát trước đó.")]
    [ProducesResponseType(typeof(AllocateBloodBagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AllocateBags(int requestId, int requestItemId, [FromBody] AllocateBloodBagsRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodRequestsService.AllocateBagsAsync(requestId, requestItemId, request, userId, ipAddress);
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
