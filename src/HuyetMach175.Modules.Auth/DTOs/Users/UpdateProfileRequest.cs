using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.DTOs.Users
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
