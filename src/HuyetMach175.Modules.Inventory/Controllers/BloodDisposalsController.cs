using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.Controllers;

[ApiController]
[Route("api/blood-disposals")]
[Authorize]
[Tags("Inventory.BloodDisposals")]
public class BloodDisposalsController : ControllerBase
{
    private readonly IBloodDisposalsService _bloodDisposalsService;

    public BloodDisposalsController(IBloodDisposalsService bloodDisposalsService)
    {
        _bloodDisposalsService = bloodDisposalsService;
    }

    [HttpGet]
    [HasPermission("DISPOSAL_VIEW")]
    [EndpointSummary("Lấy danh sách biên bản đề xuất tiêu hủy túi máu")]
    [EndpointDescription(
        "Lấy danh sách các biên bản/hồ sơ đề xuất tiêu hủy lô túi máu có phân trang và bộ lọc theo trạng thái, lý do, từ khóa tìm kiếm và khoảng thời gian.\n\n" +
        "**Bộ lọc & Phân trang:**\n" +
        "- `status`: Lọc theo trạng thái biên bản (`PENDING_APPROVAL`, `APPROVED`, `DISPOSED`, `REJECTED`, `CANCELLED`).\n" +
        "- `reason`: Lọc theo lý do tiêu hủy (`EXPIRED`, `INFECTIOUS_POSITIVE`, `HEMOLYSIS`, `DAMAGED_BAG`, `OTHER`).\n" +
        "- `search`: Tìm kiếm theo mã biên bản (`disposalCode`) hoặc mã barcode túi máu con trong lô.\n" +
        "- `fromDate` / `toDate`: Lọc theo ngày tạo đề xuất (định dạng YYYY-MM-DD).\n" +
        "- `pageNumber`: Số thứ tự trang (mặc định 1).\n" +
        "- `pageSize`: Kích thước trang, tối đa 100 (mặc định 20).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DISPOSAL_VIEW** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách biên bản tiêu hủy phân trang.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DISPOSAL_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<DisposalRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDisposals([FromQuery] DisposalFilterDto filter)
    {
        var result = await _bloodDisposalsService.GetDisposalsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission("DISPOSAL_VIEW")]
    [EndpointSummary("Lấy thông tin chi tiết một biên bản tiêu hủy kèm danh sách túi máu")]
    [EndpointDescription(
        "Lấy toàn bộ thông tin chi tiết của một biên bản đề xuất tiêu hủy túi máu theo ID, bao gồm thông tin biên bản, người đề xuất, người phê duyệt và danh sách toàn bộ các túi máu trong lô (`items`).\n\n" +
        "**Tham số đường dẫn:**\n" +
        "- `id`: Mã định danh (disposalId) của biên bản tiêu hủy.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DISPOSAL_VIEW** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết biên bản đề xuất tiêu hủy kèm danh sách các túi máu.\n" +
        "- 400 BadRequest: ID biên bản không hợp lệ (nhỏ hơn hoặc bằng 0).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DISPOSAL_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy biên bản đề xuất tiêu hủy tương ứng.")]
    [ProducesResponseType(typeof(DisposalDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDisposalById([FromRoute] int id)
    {
        var result = await _bloodDisposalsService.GetDisposalByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission("DISPOSAL_EDIT")]
    [EndpointSummary("Lập biên bản đề xuất tiêu hủy cho lô nhiều túi máu")]
    [EndpointDescription(
        "Lập biên bản đề xuất tiêu hủy cho một lô nhiều túi máu (ví dụ: hết hạn, tán huyết, rò rỉ, dương tính bệnh truyền nhiễm...).\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ định danh sách mã vạch `barcodes` của các túi máu trong lô (tối thiểu 1 túi).\n" +
        "- Tất cả túi máu phải tồn tại và không ở trạng thái đã truyền (TRANSFUSED), đã xuất (ISSUED) hoặc đã tiêu hủy (DISCARDED).\n" +
        "- Không túi máu nào được nằm trong một biên bản tiêu hủy khác đang chờ xử lý (PENDING_APPROVAL hoặc APPROVED).\n" +
        "- Khi lập biên bản thành công, trạng thái toàn bộ các túi máu trong lô được tự động chuyển sang tạm khóa (**LOCKED**) để tránh bị cấp phát hay điều chế trong lúc chờ duyệt.\n" +
        "- Tự động ghi vết vào nhật ký kiểm toán hệ thống (AuditLog).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DISPOSAL_EDIT** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Tạo biên bản tiêu hủy thành công (trả về tóm tắt thông tin biên bản).\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ hoặc trạng thái túi máu không cho phép tiêu hủy.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DISPOSAL_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy túi máu.")]
    [ProducesResponseType(typeof(CreateDisposalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDisposal([FromBody] CreateDisposalRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodDisposalsService.CreateDisposalAsync(request, userId, ipAddress);
        return CreatedAtAction(nameof(GetDisposalById), new { id = result.DisposalId }, result);
    }

    [HttpPatch("{id:int}/cancel")]
    [HasPermission("DISPOSAL_EDIT")]
    [EndpointSummary("Rút đơn / Hủy biên bản đề xuất tiêu hủy")]
    [EndpointDescription(
        "Người lập đề xuất hoặc nhân viên ngân hàng máu rút lại biên bản đề xuất tiêu hủy khi đang ở trạng thái **PENDING_APPROVAL**.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ cho phép rút đơn khi biên bản đang ở trạng thái chờ duyệt (**PENDING_APPROVAL**).\n" +
        "- Trạng thái biên bản được cập nhật sang **CANCELLED**.\n" +
        "- Toàn bộ các túi máu trong lô sẽ được tự động khôi phục lại trạng thái ban đầu (`previous_status`) trước khi bị khóa.\n" +
        "- Tự động ghi vết hoàn tác vào nhật ký kiểm toán hệ thống (AuditLog).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DISPOSAL_EDIT** (vai trò BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Rút đơn thành công, toàn bộ túi máu được hoàn trả trạng thái ban đầu.\n" +
        "- 400 BadRequest: Biên bản đã hủy hoặc đã được phê duyệt/từ chối trước đó.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DISPOSAL_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy biên bản đề xuất tiêu hủy.")]
    [ProducesResponseType(typeof(DisposalDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelDisposal([FromRoute] int id, [FromBody] CancelDisposalRequest? request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodDisposalsService.CancelDisposalAsync(id, request, userId, ipAddress);
        return Ok(result);
    }

    [HttpPatch("{id:int}/approval")]
    [HasPermission("DISPOSAL_APPROVE")]
    [EndpointSummary("Phê duyệt hoặc từ chối biên bản tiêu hủy túi máu (Dành cho Lãnh đạo)")]
    [EndpointDescription(
        "Lãnh đạo bệnh viện (MGT) duyệt hoặc từ chối biên bản tiêu hủy túi máu đang ở trạng thái **PENDING_APPROVAL**:\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Nếu đồng ý (`isApproved: true`): Trạng thái biên bản chuyển sang **APPROVED** (Đã phê duyệt - chờ KTV thực thi tiêu hủy vật lý), các túi máu vẫn giữ trạng thái tạm khóa (`LOCKED`).\n" +
        "- Nếu từ chối (`isApproved: false`): Bắt buộc phải có lý do từ chối (`rejectionReason`). Trạng thái chuyển sang **REJECTED**, toàn bộ túi máu trong lô được tự động mở khóa và khôi phục về trạng thái ban đầu.\n" +
        "- Thông tin người duyệt (`approvedBy`) và thời điểm duyệt (`approvedAt`) được tự động lưu trữ kiểm toán.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DISPOSAL_APPROVE** (vai trò MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Xét duyệt thành công, trả về chi tiết biên bản sau khi cập nhật.\n" +
        "- 400 BadRequest: Biên bản không ở trạng thái PENDING_APPROVAL hoặc thiếu lý do từ chối.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DISPOSAL_APPROVE.\n" +
        "- 404 NotFound: Không tìm thấy biên bản đề xuất tiêu hủy tương ứng.")]
    [ProducesResponseType(typeof(DisposalDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveDisposal([FromRoute] int id, [FromBody] ApproveDisposalRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodDisposalsService.ApproveDisposalAsync(id, request, userId, ipAddress);
        return Ok(result);
    }
}