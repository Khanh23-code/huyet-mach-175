using HuyetMach175.Modules.Auth.DTOs;

namespace HuyetMach175.Modules.Auth.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<UserInfoDto> GetCurrentUserAsync(int userId);
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserFilterDto filter);
    Task<UserListItemDto> GetUserByIdAsync(int userId);
    Task<UserListItemDto> CreateUserAsync(CreateUserRequest request);
    Task<UserInfoDto> UpdateCurrentUserProfileAsync(int userId, UpdateProfileRequest request);
    Task<UserListItemDto> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<UserListItemDto> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request);
    Task<UserListItemDto> UpdateUserRolesAsync(int userId, UpdateUserRolesRequest request);
    Task<bool> ResetPasswordAsync(int userId);
}