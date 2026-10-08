using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.DTOs
{
    public class UserListItemDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        public string StaffCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; }

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }   
}