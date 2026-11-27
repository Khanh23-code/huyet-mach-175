using HuyetMach175.Modules.Auth.DTOs.Common;
using HuyetMach175.Modules.Auth.DTOs.Users;

namespace HuyetMach175.Modules.Auth.Services;

public interface IUsersService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserFilterDto filter);
    Task<UserListItemDto> GetUserByIdAsync(Guid userId);
    Task<UserListItemDto> CreateUserAsync(CreateUserRequest request, bool isSysAdmin = true);
    Task<UserListItemDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<UserListItemDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request);
    Task<UserListItemDto> UpdateUserRolesAsync(Guid userId, UpdateUserRolesRequest request, bool isSysAdmin = true);
    Task<bool> ResetPasswordAsync(Guid userId);
}
