using System.Linq.Expressions;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs.Users;

namespace HuyetMach175.Modules.Auth.Mappings;

public static class UserMappings
{
    public static readonly Expression<Func<User, UserListItemDto>> ProjectToListItem = u => new UserListItemDto
    {
        UserId = u.UserId,
        StaffCode = u.StaffCode,
        Username = u.Username,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        IsActive = u.IsActive,
        DepartmentId = u.DepartmentId,
        DepartmentName = u.Department != null ? u.Department.DepartmentName : string.Empty,
        Roles = u.UserRoles.Select(ur => ur.Role!.RoleCode.ToString()).ToList()
    };
}

