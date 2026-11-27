using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.DTOs.Roles
{
    public class UpdateRolePermissionsRequest
    {
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }
}