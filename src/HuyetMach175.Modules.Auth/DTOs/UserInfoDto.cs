namespace HuyetMach175.Modules.Auth.DTOs;

public class UserInfoDto 
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}