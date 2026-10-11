using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.DTOs.Users
{
    public class UpdateUserRolesRequest
    {
        public List<int> UserRoles { get; set; } = new List<int>();
    }
}
