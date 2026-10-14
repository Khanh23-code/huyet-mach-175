using HuyetMach175.Modules.Auth.DTOs;
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
        Task<RoleDto> CreateRoleAsync(CreateRoleRequest request);
        Task<RoleDto> UpdateRoleAsync(int roleId, UpdateRoleRequest request);
        Task<RoleDto> UpdateRoleStatusAsync(int roleId, UpdateRoleStatusRequest? request = null, bool force = false);
    }
}
