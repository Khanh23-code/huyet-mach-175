using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.LabTests;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
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

    [HttpPost]
    [HasPermission("LAB_EDIT")]
    [EndpointSummary("Ghi nhận kết quả xét nghiệm 5 bệnh truyền nhiễm & Khẳng định nhóm máu")]
    [EndpointDescription(
        "Kỹ thuật viên xét nghiệm nhập kết quả sàng lọc vi sinh (HIV, HBV, HCV, Giang mai, Kháng thể bất thường) và xác nhận nhóm máu ABO/Rh cho túi máu.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Kiểm tra mã barcode hoặc bagId tồn tại và trạng thái bắt buộc phải là `READY_TESTING`.\n" +
        "- Chặn không cho nhập trùng nếu túi máu đã có kết quả xét nghiệm trước đó.\n" +
        "- Trích xuất `technicianId` từ Bearer Token.\n" +
        "- **Nếu tất cả 5 chỉ số đều Âm tính (NEGATIVE):** Kết luận `PASSED`, túi máu chuyển sang trạng thái `TESTED_PASSED`, cập nhật nhóm máu chính thức cho túi máu và hồ sơ người hiến (donors).\n" +
        "- **Nếu có ít nhất 1 chỉ số Dương tính (POSITIVE):** Kết luận `FAILED`, túi máu chuyển sang trạng thái `LOCKED` (Khóa cách ly chờ hội đồng duyệt tiêu hủy) kèm lý do trong `discard_reason`, đồng thời tự động ghi nhận cảnh báo vi sinh vào nhật ký `audit_logs`.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **LAB_EDIT** (các vai trò như BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Ghi nhận kết quả thành công, trả về chi tiết kết quả test và trạng thái mới của túi máu.\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ, túi máu không ở trạng thái READY_TESTING, hoặc túi máu đã có kết quả trước đó.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập LAB_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy túi máu.")]
    [ProducesResponseType(typeof(LabTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordLabTest([FromBody] RecordLabTestRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _labTestsService.RecordLabTestAsync(request, userId, ipAddress);
        return Ok(result);
    }

    [HttpGet("bag/{identifier}")]
    [HasPermission("LAB_VIEW")]
    [EndpointSummary("Xem chi tiết kết quả xét nghiệm của 1 túi máu")]
    [EndpointDescription(
        "Tra cứu toàn bộ kết quả xét nghiệm sàng lọc 5 bệnh truyền nhiễm, nhóm máu khẳng định ABO/Rh, tên KTV thực hiện và kết luận tổng thể của túi máu theo mã vạch barcode hoặc bagId.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Tìm kiếm linh hoạt theo chuỗi `barcode` (mã vạch túi máu) hoặc `bagId` (ID số nguyên).\n" +
        "- Trả về đầy đủ 5 chỉ số: HIV, HBV, HCV, Giang mai, Kháng thể bất thường, cùng nhóm máu khẳng định và thông tin KTV.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **LAB_VIEW** (các vai trò như BBNK, MGT, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết kết quả xét nghiệm.\n" +
        "- 400 BadRequest: Mã định danh không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập LAB_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy túi máu hoặc túi máu chưa có kết quả xét nghiệm.")]
    [ProducesResponseType(typeof(LabTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabTestByBagIdentifier([FromRoute] string identifier)
    {
        var result = await _labTestsService.GetLabTestByBagIdentifierAsync(identifier);
        return Ok(result);
    }
}
