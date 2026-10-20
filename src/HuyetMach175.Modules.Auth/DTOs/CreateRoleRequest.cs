using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs
{
    public class CreateRoleRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mã vai trò")]
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_]{1,19}$",
            ErrorMessage = "Mã vai trò 2-20 ký tự, bắt đầu bằng chữ, chỉ gồm chữ, số, dấu gạch dưới")]
        public string RoleCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên vai trò")]
        [StringLength(50, ErrorMessage = "Tên vai trò tối đa 50 ký tự")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? Description { get; set; }
    }
}