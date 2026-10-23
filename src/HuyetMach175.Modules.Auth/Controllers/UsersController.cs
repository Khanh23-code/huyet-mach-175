using HuyetMach175.Modules.Auth.DTOs.Common;
using HuyetMach175.Modules.Auth.DTOs.Users;
using HuyetMach175.Modules.Auth.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Tags("Identity.Users")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet]
        [HasPermission("USERS_VIEW")]
        [EndpointSummary("Lấy danh sách người dùng (phân trang, lọc, tìm kiếm)")]
        [EndpointDescription(
            "Trả về danh sách người dùng trong hệ thống có hỗ trợ phân trang và tìm kiếm theo nhiều tiêu chí.\n\n" +
            "- **Tìm kiếm tổng hợp:** Hỗ trợ tìm kiếm theo tên đăng nhập, họ tên, mã nhân viên, email, số điện thoại (`keyWord`).\n" +
            "- **Bộ lọc:** Lọc theo khoa/phòng ban (`departmentId`), vai trò (`roleCode`), trạng thái hoạt động (`isActive`).\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **USERS_VIEW**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về danh sách người dùng phân trang kèm tổng số lượng bản ghi." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có quyền USERS_VIEW.")]
        [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsersAsync([FromQuery] UserFilterDto filter)
        {
            var result = await _usersService.GetUsersAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [HasPermission("USERS_VIEW")]
        [EndpointSummary("Lấy thông tin chi tiết một người dùng theo ID")]
        [EndpointDescription(
            "Truy vấn thông tin chi tiết của một tài khoản người dùng theo ID (bao gồm thông tin cá nhân, mã nhân viên, khoa/phòng ban và danh sách vai trò đang được gán).\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **USERS_VIEW**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Trả về thông tin chi tiết tài khoản người dùng." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có quyền USERS_VIEW." +
            "\n- 404 Not Found: Không tìm thấy tài khoản người dùng với ID cung cấp.")]
        [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByIdAsync([FromRoute] int id)
        {
            var user = await _usersService.GetUserByIdAsync(id);
            return Ok(user);
        }

        [HttpPost]
        [HasPermission("USERS_EDIT")]
        [EndpointSummary("Tạo tài khoản người dùng mới")]
        [EndpointDescription(
            "Tạo mới một tài khoản người dùng trong hệ thống với mật khẩu được mã hóa an toàn bằng BCrypt và gán vào một khoa/phòng ban cùng các vai trò khởi tạo.\n\n" +
            "**Lưu ý nghiệp vụ:**\n" +
            "- Tên đăng nhập (`username`), mã nhân viên (`staffCode`), email và số điện thoại phải là duy nhất trên toàn hệ thống.\n" +
            "- Chỉ người dùng có vai trò **SYS** mới có quyền gán vai trò **SYS** cho tài khoản mới. Quản lý (**MGT**) không thể tự tạo tài khoản SYS.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **USERS_EDIT**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Tạo người dùng thành công, trả về thông tin tài khoản vừa tạo." +
            "\n- 400 Bad Request: Dữ liệu không hợp lệ, thiếu trường bắt buộc, khoa/phòng không tồn tại, hoặc có vai trò bị vô hiệu hóa." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Không có quyền truy cập, hoặc người dùng MGT cố tình gán vai trò SYS." +
            "\n- 409 Conflict: Dữ liệu bị trùng lặp (tên đăng nhập, mã nhân viên, email hoặc số điện thoại đã tồn tại).")]
        [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
        {
            bool isSysAdmin = User.IsInRole("SYS");
            var user = await _usersService.CreateUserAsync(request, isSysAdmin);
            return Ok(user);
        }

        [HttpPut("{id}")]
        [HasPermission("USERS_EDIT")]
        [EndpointSummary("Cập nhật thông tin người dùng theo ID")]
        [EndpointDescription(
            "Cập nhật thông tin cơ bản của người dùng (Họ và tên, Email, Số điện thoại, Khoa/phòng ban).\n\n" +
            "API này không dùng để đổi mật khẩu, thay đổi vai trò hoặc thay đổi trạng thái hoạt động.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **USERS_EDIT**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cập nhật thông tin thành công, trả về thông tin tài khoản sau khi chỉnh sửa." +
            "\n- 400 Bad Request: Dữ liệu không hợp lệ hoặc khoa/phòng ban không tồn tại." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có quyền USERS_EDIT." +
            "\n- 404 Not Found: Không tìm thấy tài khoản người dùng với ID cung cấp." +
            "\n- 409 Conflict: Email hoặc số điện thoại chỉnh sửa bị trùng với người dùng khác trong hệ thống.")]
        [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateUserAsync([FromRoute] int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _usersService.UpdateUserAsync(id, request);
            return Ok(user);
        }

        [HttpPatch("{id}/status")]
        [HasPermission("USERS_EDIT")]
        [EndpointSummary("Cập nhật trạng thái hoạt động của người dùng (Kích hoạt / Vô hiệu hóa)")]
        [EndpointDescription(
            "Thay đổi trạng thái hoạt động (`isActive`: true / false) của một tài khoản người dùng.\n\n" +
            "**Cơ chế bảo mật:** Khi vô hiệu hóa (`isActive = false`), toàn bộ Refresh Token của tài khoản đó sẽ lập tức bị thu hồi (revoke) để buộc đăng xuất khỏi toàn bộ phiên làm việc trên mọi thiết bị.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **USERS_EDIT**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cập nhật trạng thái thành công, trả về thông tin tài khoản tương ứng." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không có quyền USERS_EDIT." +
            "\n- 404 Not Found: Không tìm thấy tài khoản người dùng với ID cung cấp.")]
        [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserStatusAsync([FromRoute] int id, [FromBody] UpdateUserStatusRequest request)
        {
            var user = await _usersService.UpdateUserStatusAsync(id, request);
            return Ok(user);
        }

        [HttpPut("{id}/roles")]
        [HasPermission("USERS_EDIT")]
        [EndpointSummary("Cập nhật danh sách vai trò của người dùng")]
        [EndpointDescription(
            "Gán lại toàn bộ danh sách vai trò cho một người dùng bằng danh sách `roleIds` mới.\n\n" +
            "**Ràng buộc bảo mật & phân quyền:**\n" +
            "- Mỗi người dùng phải có ít nhất một vai trò.\n" +
            "- Chỉ tài khoản có vai trò **SYS** mới có quyền chỉnh sửa vai trò của một tài khoản SYS khác.\n" +
            "- Chỉ tài khoản có vai trò **SYS** mới có quyền gán vai trò SYS cho bất kỳ ai. Người dùng **MGT** không có quyền này.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có quyền **USERS_EDIT**.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cập nhật vai trò thành công, trả về thông tin người dùng với danh sách vai trò mới." +
            "\n- 400 Bad Request: Dữ liệu không hợp lệ, danh sách vai trò rỗng, hoặc có ID vai trò không tồn tại." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng MGT cố gắng sửa tài khoản SYS hoặc cố gắng gán vai trò SYS." +
            "\n- 404 Not Found: Không tìm thấy tài khoản người dùng với ID cung cấp.")]
        [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRolesAsync([FromRoute] int id, [FromBody] UpdateUserRolesRequest request)
        {
            bool isSysAdmin = User.IsInRole("SYS");
            var updatedUser = await _usersService.UpdateUserRolesAsync(id, request, isSysAdmin);
            return Ok(updatedUser);
        }

        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "SYS")]
        [EndpointSummary("Đặt lại mật khẩu mặc định cho người dùng")]
        [EndpointDescription(
            "Đặt lại mật khẩu của người dùng về mật khẩu mặc định ban đầu (`password123`) và đồng thời thu hồi (revoke) toàn bộ Refresh Token của người dùng đó để buộc đăng xuất khỏi các thiết bị.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Đặt lại mật khẩu thành công và thu hồi tất cả phiên làm việc." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng không phải là Quản trị hệ thống (SYS)." +
            "\n- 404 Not Found: Không tìm thấy tài khoản người dùng với ID cung cấp.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPasswordAsync([FromRoute] int id)
        {
            await _usersService.ResetPasswordAsync(id);
            return Ok(new { message = "Reset this user's password successfully." });
        }
    }
}