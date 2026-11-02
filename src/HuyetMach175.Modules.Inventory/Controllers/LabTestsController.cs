using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.LabTests;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.Controllers;

[ApiController]
[Route("api/lab-tests")]
[Authorize]
[Tags("Inventory.LabTests")]
public class LabTestsController : ControllerBase
{
    private readonly ILabTestsService _labTestsService;

    public LabTestsController(ILabTestsService labTestsService)
    {
        _labTestsService = labTestsService;
    }

    [HttpGet("queue")]
    [HasPermission("LAB_VIEW")]
    [EndpointSummary("Lấy danh sách hàng chờ xét nghiệm túi máu")]
    [EndpointDescription(
        "Lấy danh sách các túi máu đã tiếp nhận về Viện (trạng thái READY_TESTING) đang chờ Kỹ thuật viên xét nghiệm đưa vào máy chạy test vi sinh và khẳng định nhóm máu.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ truy vấn các túi máu có trạng thái `READY_TESTING` và chưa có kết quả xét nghiệm trong bảng `blood_test_results`.\n" +
        "- Sắp xếp theo thứ tự túi máu thu gom trước được xét nghiệm trước (FIFO - First In First Out).\n" +
        "- Hỗ trợ tìm kiếm theo mã barcode túi máu hoặc thông tin người hiến (`search`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **LAB_VIEW** (các vai trò như BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách hàng chờ xét nghiệm phân trang.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập LAB_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<LabTestQueueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLabTestQueue([FromQuery] LabTestQueueFilterDto filter)
    {
        var result = await _labTestsService.GetLabTestQueueAsync(filter);
        return Ok(result);
    }
}