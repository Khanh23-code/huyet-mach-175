using HuyetMach175.Api.Data;
using HuyetMach175.Modules.Auth.DTOs.Departments;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Auth.Services;

public class DepartmentsService : IDepartmentsService
{
    private readonly ApplicationDbContext _context;

    public DepartmentsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync()
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.DepartmentId)
            .Select(d => new DepartmentDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentCode = d.DepartmentCode,
                DepartmentName = d.DepartmentName
            })
            .ToListAsync();
    }
}

