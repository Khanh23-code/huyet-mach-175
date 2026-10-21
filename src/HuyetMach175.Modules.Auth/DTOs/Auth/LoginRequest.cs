using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs.Auth;

public class LoginRequest 
{
    public string? Username { get; set; }

    public string? StaffCode { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}