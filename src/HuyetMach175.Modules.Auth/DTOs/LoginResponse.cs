namespace HuyetMach175.Modules.Auth.DTOs;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpireAt { get; set; }
    public UserInfoDto UserInfo { get; set; } = new UserInfoDto();
}