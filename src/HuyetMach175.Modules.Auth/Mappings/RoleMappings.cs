using System.Linq.Expressions;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs.Roles;

namespace HuyetMach175.Modules.Auth.Mappings;

public static class RoleMappings
{
    public static readonly Expression<Func<Role, RoleDto>> ProjectToDto = r => new RoleDto
    {
        RoleId = r.RoleId,
        RoleCode = r.RoleCode,
        RoleName = r.RoleName,
        Description = r.Description,
        IsActive = r.IsActive,
        UsersCount = r.UserRoles.Count()
    };
}

