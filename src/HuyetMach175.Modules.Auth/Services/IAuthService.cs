using HuyetMach175.Modules.Auth.DTOs.Auth;
using HuyetMach175.Modules.Auth.DTOs.Common;
using HuyetMach175.Modules.Auth.DTOs.Users;

namespace HuyetMach175.Modules.Auth.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> LogoutAsync(LogoutRequest request);
    Task<RefreshTokenResponse> RefreshTokenAsync(LogoutRequest request);
    Task<UserInfoDto> GetCurrentUserAsync(int userId);
    Task<UserInfoDto> UpdateCurrentUserProfileAsync(int userId, UpdateProfileRequest request);
}