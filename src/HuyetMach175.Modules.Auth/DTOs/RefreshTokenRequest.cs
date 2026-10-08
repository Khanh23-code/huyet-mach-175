using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "RefreshToken required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
