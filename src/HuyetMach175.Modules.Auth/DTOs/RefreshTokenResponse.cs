namespace HuyetMach175.Modules.Auth.DTOs
{
    public class RefreshTokenResponse
    {
        public string NewAccessToken { get; set; } = string.Empty;
        public string NewRefreshToken { get; set; } = string.Empty;
        public DateTime NewExpiredAt { get; set; }
    }
}
