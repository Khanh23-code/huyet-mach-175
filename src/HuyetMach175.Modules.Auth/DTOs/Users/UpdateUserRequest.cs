using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs.Users
{
    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string? Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
        public string? PhoneNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa/phòng")]
        public int DepartmentId { get; set; }
    }
}