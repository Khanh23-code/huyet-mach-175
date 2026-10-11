using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs.Auth
{
    public class LogoutRequest
    {
        [Required(ErrorMessage = "RefreshToken required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
