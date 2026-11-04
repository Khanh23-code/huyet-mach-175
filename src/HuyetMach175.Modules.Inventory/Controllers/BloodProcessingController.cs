using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.Processing;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.Controllers;

[ApiController]
[Route("api/blood-processing")]
[Authorize]
[Tags("Inventory.BloodProcessing")]
public class BloodProcessingController : ControllerBase
{
    private readonly IBloodProcessingService _bloodProcessingService;

    public BloodProcessingController(IBloodProcessingService bloodProcessingService)
    {
        _bloodProcessingService = bloodProcessingService;
    }

    [HttpGet("queue")]
    [HasPermission("PROCESSING_VIEW")]
    [EndpointSummary("Lấy danh sách hàng chờ điều chế túi máu toàn phần")]
    [EndpointDescription(
        "Lấy danh sách các túi máu toàn phần (WB) đã có kết quả xét nghiệm đạt chuẩn (TESTED_PASSED) đang chờ kỹ thuật viên phân tách thành các chế phẩm con.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Chỉ lọc các túi máu toàn phần (WB) có trạng thái `TESTED_PASSED` và chưa bị điều chế (chưa có túi mẹ).\n" +
        "- Sắp xếp theo ngày thu gom tăng dần (túi thu thập trước được ưu tiên điều chế trước).\n" +
        "- Hỗ trợ tìm kiếm theo barcode hoặc tên/CCCD người hiến máu.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **PROCESSING_VIEW**.\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách hàng chờ phân trang.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập PROCESSING_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<ProcessingQueueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProcessingQueue([FromQuery] ProcessingQueueFilterDto filter)
    {
        var result = await _bloodProcessingService.GetProcessingQueueAsync(filter);
        return Ok(result);
    }

    [HttpPost("separate")]
    [HasPermission("PROCESSING_EDIT")]
    [EndpointSummary("Thực hiện ép tách túi máu mẹ thành các túi chế phẩm con")]
    [EndpointDescription(
        "Ghi nhận kết quả ly tâm và phân tách túi máu toàn phần (WB) thành các túi chế phẩm con (Hồng cầu lắng, Huyết tương đông lạnh, Khối tiểu cầu).\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Kiểm tra túi máu mẹ tồn tại và phải ở trạng thái `TESTED_PASSED`.\n" +
        "- Mã barcode túi con được tự động sinh theo chuẩn: `[Mã_Túi_Mẹ]-[Mã_Chế_Phẩm]` (ví dụ: BAG-20261101-00401-PRBC, BAG-20261101-00401-FFP, BAG-20261101-00401-PLT).\n" +
        "- Tự động tính hạn sử dụng (`expired_at`) dựa trên ngày thu gom của túi mẹ và chu kỳ bảo quản quy chuẩn:\n" +
        "  + Khối hồng cầu (PRBC): Ngày thu gom + 42 ngày (theo `life_days`).\n" +
        "  + Huyết tương đông lạnh (FFP): Ngày thu gom + 365 ngày.\n" +
        "  + Khối tiểu cầu (PLT): Ngày thu gom + 5 ngày.\n" +
        "- Các túi con được chuyển sang trạng thái `AVAILABLE` (Sẵn sàng cấp phát/lưu kho).\n" +
        "- Túi máu mẹ chuyển sang trạng thái `PROCESSED` (Đã hoàn tất điều chế) và giải phóng vị trí lưu trữ.\n" +
        "- Tự động ghi nhật ký hệ thống `audit_logs` để theo dõi truy xuất nguồn gốc.\n" +
        "- Từ chối (400) nếu: tổng thể tích con vượt thể tích túi mẹ, túi mẹ hoặc chế phẩm đã quá hạn, loại chế phẩm khai trùng, hoặc tủ lưu trữ sai nhiệt độ (PRBC 1 đến 10°C, FFP từ -18°C trở xuống, PLT 20 đến 24°C).\n" +
        "- Trả 409 nếu túi mẹ vừa được điều chế bởi người khác.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **PROCESSING_EDIT**.\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Điều chế thành công, trả về danh sách túi con kèm barcode, hạn dùng và vị trí lưu trữ.\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ, túi máu không đủ điều kiện điều chế, hoặc mã barcode đã tồn tại.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập PROCESSING_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy túi máu mẹ hoặc vị trí lưu trữ.")]
    [ProducesResponseType(typeof(SeparateBloodBagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SeparateBloodBag([FromBody] SeparateBloodBagRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodProcessingService.SeparateBloodBagAsync(request, userId, ipAddress);
        return Ok(result);
    }

    [HttpGet("component-types")]
    [HasPermission("PROCESSING_VIEW")]
    [EndpointSummary("Danh sách loại chế phẩm có thể điều chế (PRBC, FFP, PLT)")]
    [EndpointDescription("Trả về loại chế phẩm kèm hạn dùng (`lifeDays`) và nhiệt độ bảo quản để giao diện hiển thị hạn dùng dự kiến. Yêu cầu quyền **PROCESSING_VIEW**.")]
    [ProducesResponseType(typeof(List<ComponentTypeOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComponentTypes()
    {
        var result = await _bloodProcessingService.GetSeparableComponentTypesAsync();
        return Ok(result);
    }
}