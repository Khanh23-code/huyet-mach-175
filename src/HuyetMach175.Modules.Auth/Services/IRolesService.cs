using HuyetMach175.Modules.Auth.DTOs.Roles;
using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.Services
{
    public interface IRolesService 
    {
        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDto> GetRoleByIdAsync(int roleId);
    }
}
