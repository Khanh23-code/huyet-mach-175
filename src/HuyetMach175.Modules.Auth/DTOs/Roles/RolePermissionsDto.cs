using System;
using System.Collections.Generic;
using System.Text;
using HuyetMach175.Modules.Auth.DTOs.Permissions;

namespace HuyetMach175.Modules.Auth.DTOs.Roles
{
    public class RolePermissionsDto
    {
        public int RoleId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int UsersCount { get; set; }
        public List<int> AssignedPermissionIds { get; set; } = new List<int>();
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }
}
