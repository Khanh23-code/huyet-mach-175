using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.Modules.Auth.DTOs.Roles;
using HuyetMach175.Modules.Auth.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Auth.Services;

public class RolesService : IRolesService
{
    private readonly ApplicationDbContext _context;

    public RolesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleId)
            .Select(RoleMappings.ProjectToDto)
            .ToListAsync();

        return roles;
    }

    public async Task<RoleDto> GetRoleByIdAsync(int roleId)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Where(r => r.RoleId == roleId)
            .Select(RoleMappings.ProjectToDto)
            .FirstOrDefaultAsync();

        if (role == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy vai trò trong hệ thống.");
        }

        return role;
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request)
    {
        string roleCode = request.RoleCode.Trim().ToUpperInvariant();
        string roleName = request.RoleName.Trim();

        var errors = new Dictionary<string, string>();

        if (await _context.Roles.AnyAsync(r => r.RoleCode == roleCode))
            errors["roleCode"] = "Mã vai trò đã tồn tại";

        if (await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower()))
            errors["roleName"] = "Tên vai trò đã tồn tại";

        if (errors.Count > 0)
            throw new HttpException(StatusCodes.Status409Conflict, "Dữ liệu bị trùng với vai trò khác", errors);

        var newRole = new Role
        {
            RoleCode = roleCode,
            RoleName = roleName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true
        };

        _context.Roles.Add(newRole);
        await _context.SaveChangesAsync();

        return new RoleDto
        {
            RoleId = newRole.RoleId,
            RoleCode = newRole.RoleCode,
            RoleName = newRole.RoleName,
            Description = newRole.Description,
            IsActive = newRole.IsActive,
            UsersCount = 0
        };
    }

    public async Task<RoleDto> UpdateRoleAsync(int roleId, UpdateRoleRequest request)
    {
        var role = await _context.Roles.FindAsync(roleId);

        if (role == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy vai trò.");
        }

        if (string.IsNullOrWhiteSpace(request.RoleName))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng nhập tên vai trò.",
                new Dictionary<string, string> { ["roleName"] = "Vui lòng nhập tên vai trò" });
        }

        string newName = request.RoleName.Trim();

        if (newName.Length > 50)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Tên vai trò tối đa 50 ký tự.",
                new Dictionary<string, string> { ["roleName"] = "Tên vai trò tối đa 50 ký tự" });
        }

        if (await _context.Roles.AnyAsync(r => r.RoleId != roleId && r.RoleName.ToLower() == newName.ToLower()))
        {
            throw new HttpException(StatusCodes.Status409Conflict, "Dữ liệu bị trùng với vai trò khác",
                new Dictionary<string, string> { ["roleName"] = "Tên vai trò đã tồn tại" });
        }

        role.RoleName = newName;
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await _context.SaveChangesAsync();

        return await GetRoleByIdAsync(roleId);
    }

    public async Task<RoleDto> UpdateRoleStatusAsync(int roleId, UpdateRoleStatusRequest? request = null, bool force = false)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);

        if (role == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy vai trò trong hệ thống.");
        }

        bool targetStatus = request?.IsActive ?? !role.IsActive;

        if (role.IsActive == targetStatus)
        {
            return await GetRoleByIdAsync(roleId);
        }

        if (!targetStatus)
        {
            if (string.Equals(role.RoleCode, "SYS", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Không thể vô hiệu hóa vai trò quản trị hệ thống (SYS).");
            }

            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            if (userIds.Count > 0)
            {
                if (!force)
                {
                    throw new HttpException(StatusCodes.Status400BadRequest, $"Vai trò này hiện đang được gán cho {userIds.Count} người dùng. Vui lòng xác nhận với tham số force=true để tiếp tục.");
                }

                var activeTokens = await _context.RefreshTokens
                    .Where(rt => !rt.IsRevoked && userIds.Contains(rt.UserId))
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                }
            }
        }

        role.IsActive = targetStatus;
        await _context.SaveChangesAsync();

        return await GetRoleByIdAsync(roleId);
    }

    public async Task<RolePermissionsDto> GetRolePermissionsAsync(int roleId)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.RoleId == roleId);

        if (role == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy vai trò trong hệ thống.");
        }

        var assignedPermissionIds = role.RolePermissions
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var allPermissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.ModuleCode)
            .ThenBy(p => p.PermissionId)
            .ToListAsync();

        var permissions = allPermissions.Select(p =>
            PermissionMappings.ToDto(p, assignedPermissionIds.Contains(p.PermissionId))
        ).ToList();

        return new RolePermissionsDto
        {
            RoleId = role.RoleId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            Description = role.Description,
            IsActive = role.IsActive,
            UsersCount = role.UserRoles.Count,
            AssignedPermissionIds = assignedPermissionIds.OrderBy(id => id).ToList(),
            Permissions = permissions
        };
    }

    public async Task<RolePermissionsDto> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.RoleId == roleId);

        if (role == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy vai trò trong hệ thống.");
        }

        var distinctPermissionIds = (request.PermissionIds ?? new List<int>())
            .Distinct()
            .ToList();

        if (distinctPermissionIds.Count > 0)
        {
            var existingCount = await _context.Permissions
                .CountAsync(p => distinctPermissionIds.Contains(p.PermissionId));

            if (existingCount != distinctPermissionIds.Count)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Một hoặc nhiều mã quyền không hợp lệ.");
            }
        }

        var toRemove = role.RolePermissions
            .Where(rp => !distinctPermissionIds.Contains(rp.PermissionId))
            .ToList();

        foreach (var rp in toRemove)
        {
            _context.RolePermissions.Remove(rp);
        }

        var currentPermissionIds = role.RolePermissions
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        foreach (var permId in distinctPermissionIds)
        {
            if (!currentPermissionIds.Contains(permId))
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return await GetRolePermissionsAsync(roleId);
    }
}
