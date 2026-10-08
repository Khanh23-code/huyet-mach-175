using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.DTOs
{
    public class CreateUserRequest
    {
        public string StaffCode { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password {  get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber {  get; set; }
        public int DepartmentId { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>();
    }
}
