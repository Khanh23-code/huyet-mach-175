using System;

namespace HuyetMach175.Modules.Auth.DTOs.Departments
{
    public class DepartmentDto
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;

        public Guid Id => DepartmentId;
        public string Name => DepartmentName;
    }
}

