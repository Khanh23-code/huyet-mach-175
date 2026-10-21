namespace HuyetMach175.Modules.Auth.DTOs.Permissions
{
    public class CreatePermissionsResultDto
    {
        /// <summary>Các quyền vừa được tạo.</summary>
        public List<PermissionDto> Created { get; set; } = new();

        /// <summary>Các thao tác (actionType) đã tồn tại nên bị bỏ qua.</summary>
        public List<string> Skipped { get; set; } = new();
    }
}