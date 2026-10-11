using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs
{
    public class LogoutRequest
    {
        [Required(ErrorMessage = "RefreshToken required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
