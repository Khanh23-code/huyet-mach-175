using HuyetMach175.Modules.Auth.DTOs;

namespace HuyetMach175.Modules.Auth.Services;

public interface IAuthServices
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserInfoDto> GetUserInfoAsync(int userId);
}