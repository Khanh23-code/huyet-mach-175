using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.Modules.Auth.DTOs.Roles;

namespace HuyetMach175.Modules.Auth.Services;

public interface IRolesService 
{
    Task<List<RoleDto>> GetRolesAsync();
    Task<RoleDto> GetRoleByIdAsync(Guid roleId);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request);
    Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request);
    Task<RoleDto> UpdateRoleStatusAsync(Guid roleId, UpdateRoleStatusRequest? request = null, bool force = false);
    Task<RolePermissionsDto> GetRolePermissionsAsync(Guid roleId);
    Task<RolePermissionsDto> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request);
}
