using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Clinical.Controllers;

[ApiController]
[Route("api/blood-returns")]
[Authorize]
[Tags("Clinical.BloodReturns")]
public class BloodReturnsController : ControllerBase
{
    private readonly IBloodReturnsService _bloodReturnsService;

    public BloodReturnsController(IBloodReturnsService bloodReturnsService)
    {
        _bloodReturnsService = bloodReturnsService;
    }

    [HttpPost]
    [HasPermission("BLOOD_RETURN_EDIT")]
    [EndpointSummary("Lập phiếu hoàn trả túi máu chưa sử dụng")]
    [EndpointDescription(
        "Lâm sàng lập phiếu hoàn trả túi máu đã xuất kho nhưng không sử dụng và phong tỏa ngay túi máu về trạng thái `LOCKED` trong lúc vận chuyển về Ngân hàng máu.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Bắt buộc đạt cả 4 điều kiện bảng kiểm (`isUnusedFullVolume`, `isBagIntact`, `isLabelClear`, `isStoredUnder30Mins`).\n" +
        "- Chỉ người dùng thuộc cùng khoa với phiếu yêu cầu mới được lập phiếu hoàn trả; mỗi lượt phân bổ chỉ được hoàn trả tối đa 1 lần.\n" +
        "- Tạo bản ghi `blood_returns` với `qualified = false`, `final_action = PENDING_INSPECTION`.\n" +
        "- Chuyển trạng thái túi máu sang `LOCKED` và ghi vết `audit_logs`, `blood_bag_status_logs` (`ISSUED` -> `LOCKED`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_RETURN_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Lập phiếu hoàn trả thành công.\n" +
        "- 400 BadRequest: Bảng kiểm không đạt đủ 4 điều kiện, thiếu lý do hoặc trạng thái túi không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_RETURN_EDIT hoặc khác khoa điều trị.\n" +
        "- 404 NotFound: Không tìm thấy lượt phân bổ.\n" +
        "- 409 Conflict: Lượt phân bổ này đã có phiếu hoàn trả trước đó.")]
    [ProducesResponseType(typeof(CreateBloodReturnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBloodReturn([FromBody] CreateBloodReturnRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodReturnsService.CreateBloodReturnAsync(request, userId, ipAddress);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("pending")]
    [HasPermission("BLOOD_RETURN_VIEW")]
    [EndpointSummary("Lấy danh sách phiếu hoàn trả đang chờ kiểm định")]
    [EndpointDescription(
        "Trả về danh sách các phiếu hoàn trả đang ở trạng thái `PENDING_INSPECTION`, sắp xếp theo thời gian lập phiếu tăng dần (`ProcessedAt ASC`).\n\n" +
        "**Phân quyền phạm vi dữ liệu:**\n" +
        "- Vai trò `BBNK`, `MGT`, `SYS`: Xem được toàn bộ phiếu hoàn trả chờ kiểm định.\n" +
        "- Vai trò `CLIN`: Chỉ xem được các phiếu hoàn trả thuộc khoa của mình.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_RETURN_VIEW**.\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách phiếu hoàn trả chờ kiểm định phân trang.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_RETURN_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<PendingBloodReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingBloodReturns([FromQuery] PendingBloodReturnFilterDto filter)
    {
        var userId = User.GetUserId();
        var result = await _bloodReturnsService.GetPendingBloodReturnsAsync(filter, userId);
        return Ok(result);
    }

    [HttpGet("scan/{barcode}")]
    [HasPermission("BLOOD_RETURN_APPROVE")]
    [EndpointSummary("Quét barcode tra cứu túi máu hoàn trả tại Ngân hàng máu")]
    [EndpointDescription(
        "Kỹ thuật viên Ngân hàng máu quét barcode túi máu hoàn trả đang ở trạng thái `LOCKED` (`PENDING_INSPECTION`) để tải thông tin thẩm định (mã túi, nhóm máu, thể tích, hạn dùng, vị trí lưu cũ, lý do hoàn trả từ Lâm sàng).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_RETURN_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về thông tin kiểm định túi máu hoàn trả.\n" +
        "- 400 BadRequest: Mã vạch trống hoặc túi máu không ở trạng thái `LOCKED`.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_RETURN_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy túi máu hoặc phiếu hoàn trả.\n" +
        "- 409 Conflict: Phiếu hoàn trả của túi máu này đã được xử lý trước đó.")]
    [ProducesResponseType(typeof(ReturnInspectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ScanBloodReturnByBarcode(string barcode)
    {
        var userId = User.GetUserId();
        var result = await _bloodReturnsService.ScanBloodReturnByBarcodeAsync(barcode, userId);
        return Ok(result);
    }

    [HttpPut("{returnId:int}/process")]
    [HasPermission("BLOOD_RETURN_APPROVE")]
    [EndpointSummary("Thẩm định tiếp nhận hoàn trả (Tái nhập kho hoặc Tiêu hủy)")]
    [EndpointDescription(
        "Ngân hàng máu đánh giá 5 điều kiện bảo quản của túi máu hoàn trả và quyết định `RESTOCK` (tái nhập kho `AVAILABLE`) hoặc `DISCARD` (chuyển hủy `DISCARDED`).\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Chỉ xử lý phiếu hoàn trả đang ở `PENDING_INSPECTION` gắn với túi máu đang `LOCKED`; đồng thời chuyển `blood_allocations.status` sang `RETURNED`.\n" +
        "- Nhánh `RESTOCK`: Bắt buộc túi chưa hết hạn, cả 5 điều kiện bảng kiểm đạt `true`, có `newStorageLocationId` hợp lệ (đang hoạt động và chưa đầy) -> `qualified = true`, `final_action = RESTOCK`, `bag.Status = AVAILABLE`.\n" +
        "- Nhánh `DISCARD`: Không bắt buộc 5 điều kiện đạt `true` -> `qualified = false`, `final_action = DISCARD`, `bag.Status = DISCARDED`, `bag.LocationId = null`.\n" +
        "- Ghi vết đầy đủ `audit_logs` và `blood_bag_status_logs` (`LOCKED` -> `AVAILABLE` / `DISCARDED`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_RETURN_APPROVE** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Xử lý phiếu hoàn trả thành công.\n" +
        "- 400 BadRequest: Bảng kiểm/vị trí kho không hợp lệ, túi đã hết hạn khi chọn `RESTOCK`, hoặc trạng thái túi không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_RETURN_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy phiếu hoàn trả hoặc vị trí lưu trữ.\n" +
        "- 409 Conflict: Phiếu hoàn trả đã được xử lý trước đó.")]
    [ProducesResponseType(typeof(ProcessBloodReturnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProcessBloodReturn(int returnId, [FromBody] ProcessBloodReturnRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodReturnsService.ProcessBloodReturnAsync(returnId, request, userId, ipAddress);
        return Ok(result);
    }
}
