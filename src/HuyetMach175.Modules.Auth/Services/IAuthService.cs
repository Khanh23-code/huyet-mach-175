using HuyetMach175.Modules.Auth.DTOs;

namespace HuyetMach175.Modules.Auth.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserInfoDto> GetUserInfoAsync(int userId);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<PagedResult<UserListItemDto>> GetUsersAsync(UserFilterDto filter);
    Task<UserListItemDto> CreateUserAsync(CreateUserRequest request);
}