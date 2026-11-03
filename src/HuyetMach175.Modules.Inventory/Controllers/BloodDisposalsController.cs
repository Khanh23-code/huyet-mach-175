using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
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
    [EndpointSummary("Lấy danh sách phiếu đề xuất tiêu hủy túi máu")]
    [EndpointDescription(
        "Lấy danh sách các phiếu đề xuất tiêu hủy túi máu có phân trang và bộ lọc theo trạng thái, lý do, từ khóa tìm kiếm và khoảng thời gian.\n\n" +
        "**Bộ lọc & Phân trang:**\n" +
        "- `status`: Lọc theo trạng thái phiếu (`PENDING_APPROVAL`, `DISPOSED`, `REJECTED`, `CANCELLED`).\n" +
        "- `reason`: Lọc theo lý do tiêu hủy (`EXPIRED`, `INFECTIOUS_POSITIVE`, `HEMOLYSIS`, `DAMAGED_BAG`, `OTHER`).\n" +
        "- `search`: Tìm kiếm theo mã phiếu (`disposalCode`) hoặc mã barcode túi máu (`barcode`).\n" +
        "- `fromDate` / `toDate`: Lọc theo ngày tạo đề xuất (định dạng YYYY-MM-DD).\n" +
        "- `pageNumber`: Số thứ tự trang (mặc định 1).\n" +
        "- `pageSize`: Kích thước trang, tối đa 100 (mặc định 20).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DISPOSAL_VIEW** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách phiếu tiêu hủy phân trang.\n" +
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
    [EndpointSummary("Lấy thông tin chi tiết một phiếu đề xuất tiêu hủy túi máu")]
    [EndpointDescription(
        "Lấy toàn bộ thông tin chi tiết của một phiếu đề xuất tiêu hủy túi máu theo ID, bao gồm thông tin túi máu, vị trí lưu trữ, lý do tiêu hủy, người đề xuất và người phê duyệt.\n\n" +
        "**Tham số đường dẫn:**\n" +
        "- `id`: Mã định danh (disposalId) của phiếu tiêu hủy.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DISPOSAL_VIEW** (vai trò BBNK hoặc MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết phiếu đề xuất tiêu hủy.\n" +
        "- 400 BadRequest: ID phiếu không hợp lệ (nhỏ hơn hoặc bằng 0).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DISPOSAL_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy phiếu đề xuất tiêu hủy tương ứng.")]
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
}