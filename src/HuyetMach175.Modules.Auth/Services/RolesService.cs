using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.Modules.Auth.DTOs.Permissions;
using HuyetMach175.Modules.Auth.DTOs.Roles;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

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
            string roleCode = request.RoleCode.Trim().ToUpperInvariant();
            string roleName = request.RoleName.Trim();

            var errors = new Dictionary<string, string>();

            if (await _context.Roles.AnyAsync(r => r.RoleCode == roleCode))
                errors["roleCode"] = "Mã vai trò đã tồn tại";

            if (await _context.Roles.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower()))
                errors["roleName"] = "Tên vai trò đã tồn tại";

            if (errors.Count > 0)
                throw new HttpException(409, "Dữ liệu bị trùng với vai trò khác", errors);

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
                throw new HttpException(404, "Không tìm thấy vai trò.");
            }

            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                throw new HttpException(400, "Vui lòng nhập tên vai trò.",
                    new Dictionary<string, string> { ["roleName"] = "Vui lòng nhập tên vai trò" });
            }

            string newName = request.RoleName.Trim();

            if (newName.Length > 50)
            {
                throw new HttpException(400, "Tên vai trò tối đa 50 ký tự.",
                    new Dictionary<string, string> { ["roleName"] = "Tên vai trò tối đa 50 ký tự" });
            }

            if (await _context.Roles.AnyAsync(r => r.RoleId != roleId && r.RoleName.ToLower() == newName.ToLower()))
            {
                throw new HttpException(409, "Dữ liệu bị trùng với vai trò khác",
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

        public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request)
        {
            if (request == null)
            {
                throw new HttpException(400, "Dữ liệu gửi lên không hợp lệ.");
            }

            string moduleCode = request.ModuleCode.Trim().ToUpperInvariant();

            if (!Enum.TryParse<PermissionAction>(request.ActionType.Trim(), true, out var action) ||
                !Enum.IsDefined(typeof(PermissionAction), action))
            {
                throw new HttpException(400, "Thao tác không hợp lệ.",
                    new Dictionary<string, string> { ["actionType"] = "Thao tác phải là VIEW, EDIT, APPROVE hoặc CANCEL" });
            }

            string permissionCode = $"{moduleCode}_{action.ToString().ToUpperInvariant()}";

            if (await _context.Permissions.AnyAsync(p => p.PermissionCode == permissionCode))
            {
                throw new HttpException(409, $"Quyền '{permissionCode}' đã tồn tại.",
                    new Dictionary<string, string> { ["actionType"] = "Module này đã có thao tác đó" });
            }

            var existingModule = await _context.Permissions
                .FirstOrDefaultAsync(p => p.ModuleCode == moduleCode);

            string moduleName;
            if (existingModule != null)
            {
                moduleName = existingModule.ModuleName;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.ModuleName))
                {
                    throw new HttpException(400, "Module mới cần có tên hiển thị.",
                        new Dictionary<string, string> { ["moduleName"] = "Module mới cần có tên hiển thị" });
                }
                moduleName = request.ModuleName.Trim();
            }

            var permission = new Permission
            {
                ModuleCode = moduleCode,
                ModuleName = moduleName,
                ActionType = action,
                PermissionCode = permissionCode,
                PermissionName = request.PermissionName.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            return new PermissionDto
            {
                PermissionId = permission.PermissionId,
                PermissionCode = permission.PermissionCode,
                PermissionName = permission.PermissionName,
                ModuleCode = permission.ModuleCode,
                ModuleName = permission.ModuleName,
                ActionType = permission.ActionType.ToString(),
                Description = permission.Description,
                IsAssigned = false
            };
        }

        public async Task<CreatePermissionsResultDto> CreatePermissionsAsync(CreatePermissionsRequest request)
        {
            if (request == null || request.Actions == null || request.Actions.Count == 0)
            {
                throw new HttpException(400, "Vui lòng chọn ít nhất một thao tác.",
                    new Dictionary<string, string> { ["actionTypes"] = "Chọn ít nhất 1 thao tác" });
            }

            string moduleCode = request.ModuleCode.Trim().ToUpperInvariant();

            // 1) Parse + kiểm tra từng thao tác, chặn trùng ngay trong request
            var parsed = new List<(PermissionAction Action, string Name)>();
            var seen = new HashSet<PermissionAction>();
            foreach (var item in request.Actions)
            {
                if (!Enum.TryParse<PermissionAction>(item.ActionType?.Trim(), true, out var action) ||
                    !Enum.IsDefined(typeof(PermissionAction), action))
                {
                    throw new HttpException(400, "Thao tác không hợp lệ.",
                        new Dictionary<string, string> { ["actionTypes"] = "Thao tác phải là VIEW, EDIT, APPROVE hoặc CANCEL" });
                }

                if (!seen.Add(action))
                {
                    throw new HttpException(400, $"Thao tác '{action}' bị lặp trong yêu cầu.",
                        new Dictionary<string, string> { ["actionTypes"] = "Thao tác bị lặp" });
                }

                parsed.Add((action, item.PermissionName.Trim()));
            }

            // 2) Thao tác nào đã tồn tại thì bỏ qua
            var codes = parsed.Select(p => $"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}").ToList();
            var existingCodes = await _context.Permissions
                .Where(p => codes.Contains(p.PermissionCode))
                .Select(p => p.PermissionCode)
                .ToListAsync();
            var existingSet = existingCodes.ToHashSet();

            var toCreate = parsed
                .Where(p => !existingSet.Contains($"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}"))
                .ToList();

            var skipped = parsed
                .Where(p => existingSet.Contains($"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}"))
                .Select(p => p.Action.ToString().ToUpperInvariant())
                .ToList();

            if (toCreate.Count == 0)
            {
                throw new HttpException(409, "Tất cả thao tác được chọn đã tồn tại trong phân hệ này.",
                    new Dictionary<string, string> { ["actionTypes"] = "Phân hệ này đã có các thao tác đó" });
            }

            // 3) Tên module: module cũ kế thừa, module mới bắt buộc nhập
            var existingModule = await _context.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ModuleCode == moduleCode);

            string moduleName;
            if (existingModule != null)
            {
                moduleName = existingModule.ModuleName;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.ModuleName))
                {
                    throw new HttpException(400, "Module mới cần có tên hiển thị.",
                        new Dictionary<string, string> { ["moduleName"] = "Module mới cần có tên hiển thị" });
                }
                moduleName = request.ModuleName.Trim();
            }

            string? description = string.IsNullOrWhiteSpace(request.Description)
                ? existingModule?.Description
                : request.Description.Trim();

            var entities = toCreate.Select(p => new Permission
            {
                ModuleCode = moduleCode,
                ModuleName = moduleName,
                ActionType = p.Action,
                PermissionCode = $"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}",
                PermissionName = p.Name,
                Description = description
            }).ToList();

            // 4) Một SaveChanges duy nhất = một transaction: hoặc tạo hết, hoặc không tạo gì
            _context.Permissions.AddRange(entities);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Có người khác vừa tạo trùng mã quyền giữa lúc kiểm tra và lúc lưu (unique index)
                throw new HttpException(409, "Có quyền vừa được tạo bởi người khác. Vui lòng tải lại và thử lại.");
            }

            return new CreatePermissionsResultDto
            {
                Created = entities.Select(permission => new PermissionDto
                {
                    PermissionId = permission.PermissionId,
                    PermissionCode = permission.PermissionCode,
                    PermissionName = permission.PermissionName,
                    ModuleCode = permission.ModuleCode,
                    ModuleName = permission.ModuleName,
                    ActionType = permission.ActionType.ToString(),
                    Description = permission.Description,
                    IsAssigned = false
                }).ToList(),
                Skipped = skipped
            };
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