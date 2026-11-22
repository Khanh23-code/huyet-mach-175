using HuyetMach175.Modules.Inventory.DTOs.BloodBags;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.Controllers;

[ApiController]
[Route("api/blood-bags")]
[Authorize]
[Tags("Inventory.BloodBags")]
public class BloodBagsController : ControllerBase
{
    private readonly IBloodBagsService _bloodBagsService;

    public BloodBagsController(IBloodBagsService bloodBagsService)
    {
        _bloodBagsService = bloodBagsService;
    }

    [HttpGet]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Lấy danh sách túi máu trong kho hoặc chờ xét nghiệm, điều chế")]
    [EndpointDescription(
        "Lấy danh sách túi máu có phân trang và bộ lọc linh hoạt phục vụ công tác xét nghiệm, điều chế và quản lý kho.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Hỗ trợ lọc theo trạng thái (`status`): ví dụ `NEWLY_COLLECTED`, `READY_TESTING` (hoặc alias `TESTING`), `TESTED_PASSED`, `AVAILABLE`, ...\n" +
        "- Hỗ trợ lọc theo nhóm máu ABO (`bloodType`): `A`, `B`, `AB`, `O`.\n" +
        "- Hỗ trợ lọc theo yếu tố Rh (`rhFactor`): `POSITIVE`, `NEGATIVE`, `+`, `-`.\n" +
        "- Hỗ trợ lọc theo loại chế phẩm (`componentType`): `WHOLE_BLOOD`, `WB`, `PRBC`, `FFP`, `PLT` hoặc tên chế phẩm.\n" +
        "- Hỗ trợ tìm kiếm theo mã vạch (`search`) và lọc theo vị trí lưu trữ (`locationId`).\n" +
        "- Kết quả sắp xếp theo ngày thu gom mới nhất giảm dần.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW** (các vai trò như BBNK, MGT, LAB, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách túi máu phân trang.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập INVENTORY_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<BloodBagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBloodBags([FromQuery] BloodBagFilterDto filter)
    {
        var result = await _bloodBagsService.GetBloodBagsAsync(filter);
        return Ok(result);
    }

    [HttpPost("receive")]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Tiếp nhận lô túi máu từ điểm hiến về Viện")]
    [EndpointDescription(
        "Tiếp nhận túi máu đã thu gom từ điểm hiến (ở trạng thái NEWLY_COLLECTED) đưa vào kho lạnh bảo quản và sẵn sàng chờ xét nghiệm.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Kiểm tra mã barcode tồn tại trong hệ thống và trạng thái hiện tại phải là NEWLY_COLLECTED.\n" +
        "- Kiểm tra điều kiện nhiệt độ tiếp nhận (cho phép từ 2.0°C đến 10.0°C đối với máu toàn phần khi vận chuyển về Viện).\n" +
        "- Kiểm tra vị trí tủ/ngăn lưu trữ hợp lệ, đang hoạt động và chưa đầy.\n" +
        "- Cập nhật vị trí lưu trữ (storageLocationId) và chuyển trạng thái túi máu thành READY_TESTING.\n" +
        "- Tự động ghi vết tiếp nhận vào nhật ký hệ thống (AuditLog) kèm người thực hiện, nhiệt độ và vị trí lưu trữ.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_EDIT** (các vai trò như BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Tiếp nhận túi máu thành công, trả về thông tin chi tiết và trạng thái READY_TESTING.\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ, túi máu không ở trạng thái NEWLY_COLLECTED, vị trí lưu trữ không khả dụng, hoặc nhiệt độ ngoài dải an toàn.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập INVENTORY_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy barcode túi máu hoặc vị trí lưu trữ.")]
    [ProducesResponseType(typeof(ReceiveBloodBagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveBloodBag([FromBody] ReceiveBloodBagRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodBagsService.ReceiveBloodBagAsync(request, userId, ipAddress);
        return Ok(result);
    }

    [HttpGet("scan/{barcode}")]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Quét / Tra cứu thông tin và vòng đời túi máu theo Barcode")]
    [EndpointDescription(
        "Tra cứu thông tin chi tiết của một túi máu theo mã vạch barcode (dành cho máy quét hoặc nhập tay trên trang Quét vòng đời).\n\n" +
        "**Dữ liệu trả về:**\n" +
        "- Thông tin cơ bản: mã vạch, nhóm máu ABO/Rh, loại chế phẩm, thể tích, vị trí lưu trữ hiện tại.\n" +
        "- Trạng thái hiện tại (`currentStatus`, `currentStatusLabel`, `currentStep`).\n" +
        "- Trạng thái gợi ý tiếp theo (`nextStatus`, `nextStatusLabel`, `nextStep`).\n" +
        "- Các cờ cảnh báo: `isLocked` (đang tạm khóa), `isDiscarded` (đã hủy), `isExpired` (đã hết hạn sử dụng).\n" +
        "- `timeline`: Mảng 7 bước tiến trình vòng đời từ Tiếp nhận -> Cách ly -> Xét nghiệm -> Nhập kho -> Giữ chỗ -> Cấp phát -> Đã truyền kèm cờ completed và thời điểm hoàn tất.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW** (vai trò BBNK, MGT, LAB, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Tìm thấy túi máu và trả về vòng đời chi tiết.\n" +
        "- 400 BadRequest: Mã barcode không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập INVENTORY_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy túi máu với mã barcode đã cho.")]
    [ProducesResponseType(typeof(BloodBagScanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScanBloodBag([FromRoute] string barcode)
    {
        var result = await _bloodBagsService.ScanBloodBagAsync(barcode);
        return Ok(result);
    }

    [HttpPost("transition-status")]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Chuyển đổi trạng thái vòng đời túi máu qua máy quét")]
    [EndpointDescription(
        "Chuyển trạng thái túi máu sang bước tiếp theo trong quy trình kho vận hoặc chuyển vào luồng tạm khóa khẩn cấp.\n\n" +
        "**Quy tắc nghiệp vụ & Chốt chặn an toàn (Guardrails):**\n" +
        "- Chặn chuyển sang `TESTED_PASSED` thủ công (bắt buộc phải qua kết quả xét nghiệm vi sinh /api/lab-tests).\n" +
        "- Chặn chuyển sang `PROCESSED` thủ công (bắt buộc thực hiện tại quy trình ép tách chế phẩm /api/blood-processing/separate).\n" +
        "- Chặn chuyển ngược về `NEWLY_COLLECTED`.\n" +
        "- Chặn mọi thao tác nếu túi máu đã ở trạng thái tiêu hủy (`DISCARDED`) hoặc đã truyền (`TRANSFUSED`).\n" +
        "- Chặn nhập kho (`AVAILABLE`) hoặc cấp phát (`ISSUED`) nếu túi máu đã hết hạn sử dụng (`expired_at <= now`).\n" +
        "- Chặn mở khóa nếu túi máu đang nằm trong một biên bản đề xuất tiêu hủy đang chờ duyệt.\n" +
        "- Tự động ghi kép vào cả 2 bảng nhật ký: `audit_logs` (kiểm toán tổng) và `blood_bag_status_logs` (nhật ký vòng đời).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_EDIT** (vai trò BBNK, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Chuyển trạng thái thành công, trả về trạng thái mới và vị trí lưu trữ.\n" +
        "- 400 BadRequest: Vi phạm quy tắc máy trạng thái hoặc dữ liệu không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập INVENTORY_EDIT.\n" +
        "- 404 NotFound: Không tìm thấy túi máu hoặc vị trí tủ lưu trữ.")]
    [ProducesResponseType(typeof(TransitionBagStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransitionStatus([FromBody] TransitionBagStatusRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodBagsService.TransitionStatusAsync(request, userId, ipAddress);
        return Ok(result);
    }

    [HttpGet("audit-logs/recent")]
    [HasPermission("INVENTORY_VIEW")]
    [EndpointSummary("Lấy danh sách nhật ký chuyển trạng thái túi máu gần đây")]
    [EndpointDescription(
        "Lấy danh sách lịch sử chuyển trạng thái túi máu gần đây nhất phục vụ bảng 'Nhật ký gần đây' bên dưới màn hình quét barcode.\n\n" +
        "**Tham số Query:**\n" +
        "- `limit`: Số lượng bản ghi cần lấy (mặc định 10, tối đa 100).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **INVENTORY_VIEW** (vai trò BBNK, MGT, LAB, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách nhật ký gần đây kèm thông tin người thao tác và thời điểm.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập INVENTORY_VIEW.")]
    [ProducesResponseType(typeof(RecentStatusLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentAuditLogs([FromQuery] int limit = 10)
    {
        var result = await _bloodBagsService.GetRecentStatusLogsAsync(limit);
        return Ok(result);
    }

    [HttpPost("external")]
    [HasPermission("INVENTORY_EDIT")]
    [EndpointSummary("Khởi tạo mã định danh cho túi máu tiếp nhận từ đơn vị ngoài")]
    [EndpointDescription(
        "Tạo túi máu toàn phần (WB) nhận từ đơn vị ngoài, sinh barcode `EXT-{yyyyMMdd}-{BagId:D6}`, lưu vào tủ và chuyển thẳng sang READY_TESTING.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Bắt buộc: đơn vị cung cấp, mã túi gốc của đơn vị ngoài (chống nhận trùng theo cặp đơn vị + mã), nhóm máu, thể tích, thời gian thu thập, nhiệt độ tiếp nhận (2.0 đến 10.0°C), vị trí lưu trữ.\n" +
        "- Hạn dùng = thời gian thu thập + `life_days` của WB. Từ chối nếu đã quá hạn.\n" +
        "- Vị trí lưu trữ phải đang hoạt động, chưa đầy và có nhiệt độ mục tiêu phù hợp với WB.\n" +
        "- Nhóm máu khai báo chỉ là dự kiến, xét nghiệm sẽ khẳng định lại.\n" +
        "- Ghi `audit_logs` (INSERT).\n\n" +
        "**Yêu cầu quyền truy cập:** **INVENTORY_EDIT**.\n\n" +
        "**HTTP Response:** 200 OK, 400 dữ liệu không hợp lệ, 404 không thấy vị trí, 409 trùng mã túi ngoài.")]
    [ProducesResponseType(typeof(RegisterExternalBloodBagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterExternalBloodBag([FromBody] RegisterExternalBloodBagRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodBagsService.RegisterExternalBloodBagAsync(request, userId, ipAddress);
        return Ok(result);
    }
}
