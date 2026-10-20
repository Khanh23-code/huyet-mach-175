using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.Modules.Auth.DTOs.Permissions;
using HuyetMach175.Modules.Auth.DTOs.Roles;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "SYS")]
    [Tags("Identity.Roles")]
    public class RolesController : ControllerBase
    {
        private readonly IRolesService _rolesService;
        
        public RolesController(IRolesService rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        [Authorize(Roles = "SYS,MGT")]
        [EndpointSummary("Lấy danh sách tất cả vai trò")]
        [EndpointDescription(
            "Trả về danh sách tất cả các vai trò (roles) trong hệ thống, bao gồm mã vai trò, tên hiển thị, mô tả, trạng thái hoạt động và số lượng người dùng đang được gán vào mỗi vai trò.\n\n" +
            "Thường dùng để hiển thị trên màn hình quản lý vai trò hoặc đổ dữ liệu vào các ô chọn (select / dropdown) khi phân quyền người dùng.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống) hoặc **MGT** (Ban Giám đốc / Quản lý).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về danh sách tất cả vai trò trong hệ thống." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có vai trò SYS hoặc MGT.")]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRolesAsync()
        {
            var roles = await _rolesService.GetRolesAsync();
            return Ok(roles);
        }

        [HttpGet("{roleId}")]
        [EndpointSummary("Lấy thông tin chi tiết một vai trò theo ID")]
        [EndpointDescription(
            "Truy vấn thông tin chi tiết của một vai trò theo `roleId` (bao gồm mã vai trò, tên vai trò, mô tả, trạng thái hoạt động và số lượng tài khoản đang giữ vai trò này).\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về thông tin chi tiết vai trò." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 404 Not Found: Không tìm thấy vai trò với ID cung cấp.")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleByIdAsync([FromRoute] int roleId)
        {
            var role = await _rolesService.GetRoleByIdAsync(roleId);
            return Ok(role);
        }

        [HttpPost]
        [EndpointSummary("Tạo một vai trò mới")]
        [EndpointDescription(
            "Tạo mới một vai trò trong hệ thống với trạng thái mặc định ban đầu là kích hoạt (`isActive = true`).\n\n" +
            "**Ràng buộc nghiệp vụ:**\n" +
            "- Mã vai trò (`roleCode`) và tên vai trò (`roleName`) phải là duy nhất trên toàn hệ thống.\n" +
            "- Mã vai trò sau khi tạo sẽ là định danh bất biến, không thể sửa đổi.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Tạo vai trò mới thành công, trả về thông tin vai trò vừa tạo." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ hoặc thiếu thông tin bắt buộc." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 409 Conflict: Mã vai trò hoặc tên vai trò đã tồn tại trong hệ thống.")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateRoleAsync([FromBody] CreateRoleRequest request)
        {
            var newRole = await _rolesService.CreateRoleAsync(request);
            return Ok(newRole);
        }

        [HttpPut("{roleId}")]
        [EndpointSummary("Cập nhật tên và mô tả vai trò")]
        [EndpointDescription(
            "Cho phép cập nhật tên hiển thị (`roleName`) và mô tả (`description`) của một vai trò hiện có. Mã vai trò (`roleCode`) là định danh bất biến, không được phép chỉnh sửa.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cập nhật thông tin vai trò thành công." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ hoặc tên vai trò vượt quá 50 ký tự." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 404 Not Found: Không tìm thấy vai trò với ID cung cấp." +
            "\n- 409 Conflict: Tên vai trò chỉnh sửa bị trùng với một vai trò khác trong hệ thống.")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateRoleAsync([FromRoute] int roleId, [FromBody] UpdateRoleRequest request)
        {
            var updatedRole = await _rolesService.UpdateRoleAsync(roleId, request);
            return Ok(updatedRole);
        }

        [HttpPatch("{roleId}/status")]
        [EndpointSummary("Cập nhật trạng thái hoạt động của vai trò (Kích hoạt / Vô hiệu hóa)")]
        [EndpointDescription(
            "Kích hoạt hoặc vô hiệu hóa trạng thái hoạt động (`isActive`) của một vai trò trong hệ thống.\n\n" +
            "**Cơ chế & Ràng buộc bảo mật:**\n" +
            "- Tuyệt đối không thể vô hiệu hóa vai trò quản trị hệ thống tối cao (**SYS**).\n" +
            "- Nếu vai trò đang có người dùng gán (`usersCount > 0`), API bắt buộc phải truyền query parameter `force=true` để xác nhận. Khi đó, toàn bộ Refresh Token của các tài khoản đang giữ vai trò này sẽ lập tức bị thu hồi (revoke) để buộc đăng xuất khỏi hệ thống nhằm đảm bảo an toàn phân quyền.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cập nhật trạng thái vai trò thành công." +
            "\n- 400 Bad Request: Cố tình vô hiệu hóa vai trò SYS, hoặc vai trò đang có người dùng nhưng chưa truyền `force=true`." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 404 Not Found: Không tìm thấy vai trò với ID cung cấp.")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRoleStatusAsync(
            [FromRoute] int roleId,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] UpdateRoleStatusRequest? request = null,
            [FromQuery] bool force = false)
        {
            var updatedRole = await _rolesService.UpdateRoleStatusAsync(roleId, request, force);
            return Ok(updatedRole);
        }

        [HttpGet("{roleId}/permissions")]
        [EndpointSummary("Lấy ma trận quyền hạn (permissions) của một vai trò")]
        [EndpointDescription(
            "Trả về toàn bộ danh sách quyền hạn trong hệ thống kèm trạng thái đã được gán (`isAssigned = true/false`) cho vai trò tương ứng và danh sách ID các quyền đang được gán (`assignedPermissionIds`).\n\n" +
            "Dùng để render bảng ma trận phân quyền (Permission Matrix) trực quan trên giao diện quản trị phân quyền.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về ma trận danh sách quyền hạn của vai trò." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 404 Not Found: Không tìm thấy vai trò với ID cung cấp.")]
        [ProducesResponseType(typeof(RolePermissionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolePermissionsAsync([FromRoute] int roleId)
        {
            var result = await _rolesService.GetRolePermissionsAsync(roleId);
            return Ok(result);
        }

        [HttpPut("{roleId}/permissions")]
        [EndpointSummary("Cập nhật danh sách quyền hạn được gán cho vai trò")]
        [EndpointDescription(
            "Gán lại toàn bộ danh sách quyền hạn cho vai trò bằng danh sách `permissionIds` mới. Những quyền cũ không còn trong danh sách sẽ bị gỡ bỏ, những quyền mới sẽ được thêm vào vai trò.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cập nhật quyền hạn cho vai trò thành công, trả về ma trận quyền hạn sau khi cập nhật." +
            "\n- 400 Bad Request: Dữ liệu không hợp lệ hoặc có ID quyền hạn không tồn tại trong hệ thống." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 404 Not Found: Không tìm thấy vai trò với ID cung cấp.")]
        [ProducesResponseType(typeof(RolePermissionsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRolePermissionsAsync([FromRoute] int roleId, [FromBody] UpdateRolePermissionsRequest request)
        {
            var result = await _rolesService.UpdateRolePermissionsAsync(roleId, request);
            return Ok(result);
        }
    }
}
