using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Auth.DTOs;

public class LoginRequest 
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}