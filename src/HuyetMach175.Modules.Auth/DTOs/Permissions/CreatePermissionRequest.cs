using System;

namespace HuyetMach175.Modules.Auth.DTOs.Permissions
{
    public class CreatePermissionRequest
    {
        public string ModuleCode { get; set; } = string.Empty;
        public string? ModuleName { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}