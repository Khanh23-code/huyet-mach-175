namespace HuyetMach175.Modules.Auth.DTOs
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int DepartmentId { get; set; }
    }
}
