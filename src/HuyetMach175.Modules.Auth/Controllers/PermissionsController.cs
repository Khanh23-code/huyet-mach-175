using HuyetMach175.Modules.Auth.DTOs.Permissions;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    [Authorize(Roles = "SYS")]
    [Tags("Identity.Permissions")]
    public class PermissionsController : ControllerBase
    {
        private readonly IRolesService _rolesService;

        public PermissionsController(IRolesService rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        [EndpointSummary("Lấy danh sách tất cả các quyền hạn trong hệ thống")]
        [EndpointDescription(
            "Trả về toàn bộ danh mục các quyền hạn (permissions) được định nghĩa trong hệ thống, bao gồm mã quyền, tên quyền, mã module, tên module, loại thao tác (`VIEW`, `EDIT`, `APPROVE`, `CANCEL`) và mô tả chi tiết.\n\n" +
            "Thường dùng để hiển thị danh mục quyền của hệ thống hoặc phục vụ cấu hình cấp quyền động.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về danh sách tất cả các quyền hạn trong hệ thống." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS).")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPermissionsAsync()
        {
            var permissions = await _rolesService.GetAllPermissionsAsync();
            return Ok(permissions);
        }

        [HttpPost]
        [EndpointSummary("Tạo một quyền hạn mới trong hệ thống")]
        [EndpointDescription(
            "Cho phép Quản trị hệ thống (SYS) định nghĩa thêm một quyền hạn mới gắn với một module nghiệp vụ cụ thể.\n\n" +
            "**Quy tắc nghiệp vụ & Mã hóa:**\n" +
            "- Loại thao tác (`actionType`) hợp lệ: `VIEW` (Xem), `EDIT` (Chỉnh sửa/Thêm mới/Xóa), `APPROVE` (Phê duyệt), `CANCEL` (Hủy bỏ).\n" +
            "- Mã quyền được hệ thống sinh tự động theo công thức: `{moduleCode}_{actionType}` (Ví dụ: `DONOR_VIEW`, `CAMPAIGN_APPROVE`).\n" +
            "- Nếu module đã tồn tại trong hệ thống, `moduleName` sẽ được tự động kế thừa từ module đó.\n" +
            "- Mã quyền sinh ra không được trùng lặp với bất kỳ quyền nào đã có trong hệ thống.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Tạo quyền hạn mới thành công, trả về thông tin chi tiết quyền vừa tạo." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ, thao tác không đúng (VIEW/EDIT/APPROVE/CANCEL), hoặc thiếu tên hiển thị cho module mới." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 409 Conflict: Quyền hạn với mã `{moduleCode}_{actionType}` đã tồn tại trong hệ thống.")]
        [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreatePermissionAsync([FromBody] CreatePermissionRequest request)
        {
            var createdPermission = await _rolesService.CreatePermissionAsync(request);
            return Ok(createdPermission);
        }
    }
}
