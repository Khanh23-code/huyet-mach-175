using HuyetMach175.Modules.Auth.DTOs.Auth;
using HuyetMach175.Modules.Auth.DTOs.Common;
using HuyetMach175.Modules.Auth.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.Services
{
    public interface IUsersService
    {
        Task<PagedResult<UserListItemDto>> GetUsersAsync(UserFilterDto filter);
        Task<UserListItemDto> GetUserByIdAsync(int userId);
        Task<UserListItemDto> CreateUserAsync(CreateUserRequest request, bool isSysAdmin = true);
        Task<UserListItemDto> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<UserListItemDto> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request);
        Task<UserListItemDto> UpdateUserRolesAsync(int userId, UpdateUserRolesRequest request, bool isSysAdmin = true);
        Task<bool> ResetPasswordAsync(int userId);
    }
}
