using System.Linq.Expressions;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs.Permissions;

namespace HuyetMach175.Modules.Auth.Mappings;

public static class PermissionMappings
{
    public static readonly Expression<Func<Permission, PermissionDto>> ProjectToDto = p => new PermissionDto
    {
        PermissionId = p.PermissionId,
        PermissionCode = p.PermissionCode,
        PermissionName = p.PermissionName,
        ModuleCode = p.ModuleCode,
        ModuleName = p.ModuleName,
        ActionType = p.ActionType.ToString(),
        Description = p.Description,
        IsAssigned = false
    };

    public static PermissionDto ToDto(Permission p, bool isAssigned = false) => new()
    {
        PermissionId = p.PermissionId,
        PermissionCode = p.PermissionCode,
        PermissionName = p.PermissionName,
        ModuleCode = p.ModuleCode,
        ModuleName = p.ModuleName,
        ActionType = p.ActionType.ToString(),
        Description = p.Description,
        IsAssigned = isAssigned
    };
}

