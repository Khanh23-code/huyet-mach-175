using HuyetMach175.Modules.Auth.DTOs.Departments;

namespace HuyetMach175.Modules.Auth.Services;

public interface IDepartmentsService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync();
}

