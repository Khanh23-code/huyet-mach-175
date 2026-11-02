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
}
