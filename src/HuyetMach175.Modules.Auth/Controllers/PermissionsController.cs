using HuyetMach175.Modules.Auth.DTOs.Permissions;
using HuyetMach175.Modules.Auth.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    [Authorize]
    [Tags("Identity.Permissions")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionsService _permissionsService;

        public PermissionsController(IPermissionsService permissionsService)
        {
            _permissionsService = permissionsService;
        }

        [HttpGet]
        [HasPermission("ROLES_VIEW")]
        [EndpointSummary("Lấy danh sách tất cả các quyền hạn trong hệ thống")]
        [EndpointDescription(
            "Trả về toàn bộ danh mục các quyền hạn (permissions) được định nghĩa trong hệ thống, bao gồm mã quyền, tên quyền, mã module, tên module, loại thao tác (`VIEW`, `EDIT`, `APPROVE`, `CANCEL`) và mô tả chi tiết.\n\n" +
            "Thường dùng để hiển thị danh mục quyền của hệ thống hoặc phục vụ cấu hình cấp quyền động.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **ROLES_VIEW**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về danh sách tất cả các quyền hạn trong hệ thống." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có quyền ROLES_VIEW.")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPermissionsAsync()
        {
            var permissions = await _permissionsService.GetAllPermissionsAsync();
            return Ok(permissions);
        }

        [HttpPost]
        [HasPermission("ROLES_EDIT")]
        [EndpointSummary("Tạo một quyền hạn mới trong hệ thống")]
        [EndpointDescription(
            "Cho phép Quản trị hệ thống (SYS) định nghĩa thêm một quyền hạn mới gắn với một module nghiệp vụ cụ thể.\n\n" +
            "**Quy tắc nghiệp vụ & Mã hóa:**\n" +
            "- Loại thao tác (`actionType`) hợp lệ: `VIEW` (Xem), `EDIT` (Chỉnh sửa/Thêm mới/Xóa), `APPROVE` (Phê duyệt), `CANCEL` (Hủy bỏ).\n" +
            "- Mã quyền được hệ thống sinh tự động theo công thức: `{moduleCode}_{actionType}` (Ví dụ: `DONOR_VIEW`, `CAMPAIGN_APPROVE`).\n" +
            "- Nếu module đã tồn tại trong hệ thống, `moduleName` sẽ được tự động kế thừa từ module đó.\n" +
            "- Mã quyền sinh ra không được trùng lặp với bất kỳ quyền nào đã có trong hệ thống.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **ROLES_EDIT**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Tạo quyền hạn mới thành công, trả về thông tin chi tiết quyền vừa tạo." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ, thao tác không đúng (VIEW/EDIT/APPROVE/CANCEL), hoặc thiếu tên hiển thị cho module mới." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có quyền ROLES_EDIT." +
            "\n- 409 Conflict: Quyền hạn với mã `{moduleCode}_{actionType}` đã tồn tại trong hệ thống.")]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePermissionAsync([FromBody] CreatePermissionRequest request)
        {
            var createdPermission = await _permissionsService.CreatePermissionAsync(request);
            return Ok(createdPermission);
        }

        [HttpPost("batch")]
        [HasPermission("ROLES_EDIT")]
        [EndpointSummary("Tạo nhiều quyền (nhiều thao tác) của cùng một module trong một lần")]
        [EndpointDescription(
            "Tạo đồng thời nhiều quyền hạn cho **một** module trong **một transaction** (hoặc tạo hết, hoặc không tạo gì).\n\n" +
            "**Quy tắc nghiệp vụ:**\n" +
            "- Mỗi phần tử trong `actions` gồm `actionType` (`VIEW`, `EDIT`, `APPROVE`, `CANCEL`) và `permissionName`. Mã quyền sinh theo `{moduleCode}_{actionType}`.\n" +
            "- Module đã tồn tại thì `moduleName` được kế thừa; module mới bắt buộc có `moduleName`.\n" +
            "- Thao tác đã tồn tại sẽ **được bỏ qua** (trả về trong `skipped`), các thao tác còn lại vẫn được tạo.\n" +
            "- Nếu mọi thao tác yêu cầu đều đã tồn tại, trả về 409 Conflict.\n" +
            "- `actionType` trùng lặp trong cùng một request bị coi là dữ liệu không hợp lệ (400).\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **ROLES_EDIT**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về `created` (các quyền vừa tạo) và `skipped` (các thao tác đã tồn tại)." +
            "\n- 400 Bad Request: Dữ liệu không hợp lệ, thao tác sai, trùng thao tác trong request, hoặc thiếu tên module mới." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có quyền ROLES_EDIT." +
            "\n- 409 Conflict: Tất cả thao tác yêu cầu đều đã tồn tại trong module.")]
        [ProducesResponseType(typeof(CreatePermissionsResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePermissionsAsync([FromBody] CreatePermissionsRequest request)
        {
            var result = await _permissionsService.CreatePermissionsAsync(request);
            return Ok(result);
        }
    }
}