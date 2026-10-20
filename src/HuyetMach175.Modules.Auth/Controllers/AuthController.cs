using HuyetMach175.Modules.Auth.DTOs.Auth;
using HuyetMach175.Modules.Auth.DTOs.Users;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Identity.Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [EndpointSummary("Đăng nhập hệ thống")]
        [EndpointDescription(
            "Xác thực tài khoản người dùng bằng tên đăng nhập (Username) và mật khẩu (Password).\n\n" +
            "Nếu thành công, trả về JWT Access Token (dùng để xác thực các request tiếp theo), Refresh Token (dùng để làm mới Access Token) và thông tin chi tiết hồ sơ người dùng kèm danh sách quyền hạn.\n\n" +
            "**Yêu cầu quyền truy cập:** Công khai (Public / Không yêu cầu đăng nhập).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Đăng nhập thành công, trả về Access Token, Refresh Token và thông tin người dùng." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không đúng định dạng hoặc thiếu các trường bắt buộc." +
            "\n- 401 Unauthorized: Tên đăng nhập hoặc mật khẩu không chính xác, hoặc tài khoản đã bị vô hiệu hóa.")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        [EndpointSummary("Đăng xuất hệ thống")]
        [EndpointDescription(
            "Thực hiện đăng xuất phiên làm việc hiện tại của người dùng bằng cách thu hồi (revoke) Refresh Token tương ứng trong cơ sở dữ liệu để ngăn chặn việc tái sử dụng.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng đang đăng nhập.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Đăng xuất thành công, Refresh Token đã bị thu hồi." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ hoặc thiếu Refresh Token." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _authService.LogoutAsync(request);
            return Ok(new { message = "Logout successfully." });
        }

        [HttpPost("refresh-token")]
        [EndpointSummary("Làm mới Access Token")]
        [EndpointDescription(
            "Cấp mới một cặp Access Token và Refresh Token khi Access Token hiện tại đã hết hạn.\n\n" +
            "Hệ thống áp dụng cơ chế Refresh Token Rotation: mỗi khi cấp mới thành công, Refresh Token cũ sẽ bị lập tức thu hồi và thay thế bằng một Refresh Token mới để đảm bảo tính an toàn.\n\n" +
            "**Yêu cầu quyền truy cập:** Công khai (Public / Client gửi kèm Refresh Token trong body).\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cấp mới token thành công, trả về Access Token mới và Refresh Token mới." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ hoặc thiếu Refresh Token." +
            "\n- 401 Unauthorized: Refresh Token không tồn tại, đã hết hạn hoặc đã bị thu hồi trước đó.")]
        [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] LogoutRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }

        [HttpGet("me")]
        [Authorize]
        [EndpointSummary("Lấy thông tin tài khoản đang đăng nhập")]
        [EndpointDescription(
            "Trích xuất định danh người dùng từ Access Token của phiên hiện tại và trả về toàn bộ thông tin chi tiết hồ sơ cá nhân, bao gồm mã nhân viên, họ tên, email, số điện thoại, khoa/phòng ban, danh sách vai trò (roles) và toàn bộ quyền hạn (permissions) được phân quyền.\n\n" +
            "Thường được gọi ngay sau khi ứng dụng frontend khởi tạo hoặc sau khi đăng nhập thành công để thiết lập trạng thái người dùng và phân quyền giao diện (Route Guard, Menu, Nút bấm).\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng đang đăng nhập.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Lấy thông tin thành công, trả về chi tiết hồ sơ và quyền hạn người dùng." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc Access Token không hợp lệ / đã hết hạn." +
            "\n- 404 Not Found: Không tìm thấy tài khoản người dùng tương ứng trong cơ sở dữ liệu.")]
        [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userInfo = await _authService.GetCurrentUserAsync(userId);
            return Ok(userInfo);
        }

        [HttpPut("me")]
        [Authorize]
        [EndpointSummary("Cập nhật thông tin cá nhân")]
        [EndpointDescription(
            "Cho phép người dùng đang đăng nhập tự cập nhật thông tin cá nhân cơ bản của chính mình (Họ và tên, Email, Số điện thoại).\n\n" +
            "API không cho phép tự sửa đổi vai trò (Role), khoa/phòng ban (Department) hoặc quyền hạn (Permissions).\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng đang đăng nhập.\n\n" +
            "**HTTP Response:**" +
            "\n- 200 OK: Cập nhật thành công, trả về thông tin hồ sơ người dùng sau khi sửa đổi." +
            "\n- 400 Bad Request: Dữ liệu gửi lên không hợp lệ (họ tên rỗng, định dạng email/số điện thoại không hợp lệ)." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc Access Token không hợp lệ / đã hết hạn." +
            "\n- 404 Not Found: Không tìm thấy tài khoản người dùng tương ứng trong cơ sở dữ liệu.")]
        [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCurrentUserProfileAsync([FromBody] UpdateProfileRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var updatedProfile = await _authService.UpdateCurrentUserProfileAsync(userId, request);

            return Ok(updatedProfile);
        }
    }
}
