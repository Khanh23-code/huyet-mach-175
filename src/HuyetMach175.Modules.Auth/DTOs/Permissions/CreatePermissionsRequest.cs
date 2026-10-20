using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs.Permissions
{
    /// <summary>Tạo nhiều quyền (nhiều thao tác) của cùng một module trong một lần gọi.</summary>
    public class CreatePermissionsRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mã module")]
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_]{1,19}$",
            ErrorMessage = "Mã module 2-20 ký tự, bắt đầu bằng chữ, chỉ gồm chữ, số, dấu gạch dưới")]
        public string ModuleCode { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Tên module tối đa 100 ký tự")]
        public string? ModuleName { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một thao tác")]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một thao tác")]
        public List<CreatePermissionActionItem> Actions { get; set; } = new();
    }

    public class CreatePermissionActionItem
    {
        [Required(ErrorMessage = "Vui lòng chọn thao tác")]
        public string ActionType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên quyền")]
        [StringLength(100, ErrorMessage = "Tên quyền tối đa 100 ký tự")]
        public string PermissionName { get; set; } = string.Empty;
    }
}