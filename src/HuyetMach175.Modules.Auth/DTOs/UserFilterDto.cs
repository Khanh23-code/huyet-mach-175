using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.DTOs
{
    public class UserFilterDto
    {
        public string? KeyWord { get; set; }
        public int? DepartmentId { get; set; }
        public string? RoleCode { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
