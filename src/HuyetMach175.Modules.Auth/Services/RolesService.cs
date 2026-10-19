using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.Modules.Auth.DTOs.Permissions;
using HuyetMach175.Modules.Auth.DTOs.Roles;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HuyetMach175.Modules.Auth.Services
{
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
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleCode = r.RoleCode,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UsersCount = r.UserRoles.Count()
                })
                .ToListAsync();

            return roles;
        }

        public async Task<RoleDto> GetRoleByIdAsync(int roleId)
        {
            var role = await _context.Roles
                .AsNoTracking()
                .Where(r => r.RoleId == roleId)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleCode = r.RoleCode,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UsersCount = r.UserRoles.Count()
                })
                .FirstOrDefaultAsync();

            if (role == null)
            {
                throw new HttpException(404, "This role was not found in the system.");
            }

            return role;
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleCode))
            {
                throw new HttpException(400, "Role code is required.");
            }

            if (request.RoleCode.Trim().Length > 50)
            {
                throw new HttpException(400, "Role code must not exceed 50 characters.");
            }

            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                throw new HttpException(400, "Role name is required.");
            }

            if (request.RoleName.Trim().Length > 50)
            {
                throw new HttpException(400, "Role name must not exceed 50 characters.");
            }

            string roleCode = request.RoleCode.Trim().ToUpper();

            if (await _context.Roles.AnyAsync(r => r.RoleCode == roleCode))
            {
                throw new HttpException(400, "This role code already exists in the system.");
            }

            var newRole = new Role
            {
                RoleCode = roleCode,
                RoleName = request.RoleName.Trim(),
                Description = request.Description?.Trim(),
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
                throw new HttpException(404, "This role was not found in the system.");
            }

            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                throw new HttpException(400, "Role name is required.");
            }

            if (request.RoleName.Trim().Length > 50)
            {
                throw new HttpException(400, "Role name must not exceed 50 characters.");
            }

            role.RoleName = request.RoleName.Trim();
            role.Description = request.Description?.Trim();

            await _context.SaveChangesAsync();

            return await GetRoleByIdAsync(roleId);
        }

        public async Task<RoleDto> UpdateRoleStatusAsync(int roleId, UpdateRoleStatusRequest? request = null, bool force = false)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                throw new HttpException(404, "This role was not found in the system.");
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
                    throw new HttpException(400, "Cannot deactivate the system administrator role (SYS).");
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
                        throw new HttpException(400, $"This role currently has {userIds.Count} user(s) assigned. Please confirm with force=true to proceed.");
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
                throw new HttpException(404, "This role was not found in the system.");
            }

            var assignedPermissionIds = role.RolePermissions
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.ModuleCode)
                .ThenBy(p => p.PermissionId)
                .ToListAsync();

            var permissions = allPermissions.Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionCode = p.PermissionCode,
                PermissionName = p.PermissionName,
                ModuleCode = p.ModuleCode,
                ModuleName = p.ModuleName,
                ActionType = p.ActionType.ToString(),
                Description = p.Description,
                IsAssigned = assignedPermissionIds.Contains(p.PermissionId)
            }).ToList();

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

        public async Task<List<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.ModuleCode)
                .ThenBy(p => p.PermissionId)
                .Select(p => new PermissionDto
                {
                    PermissionId = p.PermissionId,
                    PermissionCode = p.PermissionCode,
                    PermissionName = p.PermissionName,
                    ModuleCode = p.ModuleCode,
                    ModuleName = p.ModuleName,
                    ActionType = p.ActionType.ToString(),
                    Description = p.Description,
                    IsAssigned = false
                })
                .ToListAsync();

            return permissions;
        }

        public async Task<RolePermissionsDto> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                throw new HttpException(404, "This role was not found in the system.");
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
                    throw new HttpException(400, "One or more permission IDs are invalid.");
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
}
