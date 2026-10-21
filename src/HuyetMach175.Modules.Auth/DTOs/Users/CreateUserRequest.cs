using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs.Users
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên")]
        [RegularExpression(@"^NV\d{5}$", ErrorMessage = "Mã nhân viên có dạng NV12345")]
        public string StaffCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ, số, dấu . _ -")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 100 ký tự")]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string? Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
        public string? PhoneNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa/phòng")]
        public int DepartmentId { get; set; }

        [MinLength(1, ErrorMessage = "Chọn ít nhất 1 vai trò")]
        public List<int> RoleIds { get; set; } = new();
    }
}