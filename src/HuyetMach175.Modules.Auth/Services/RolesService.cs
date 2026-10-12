using HuyetMach175.Api.Data;
using HuyetMach175.Modules.Auth.DTOs.Roles;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace HuyetMach175.Modules.Auth.Services
{
    public class RolesService : IRolesService
    {
        private readonly ApplicationDbContext _context;

        public RolesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleId)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleCode = r.RoleCode,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UsersCount = r.UserRoles.Count()
                })
                .ToListAsync();

            return roles;
        }

        public async Task<RoleDto> GetRoleByIdAsync(int roleId)
        {
            var role = await _context.Roles
                .AsNoTracking()
                .Where(r => r.RoleId == roleId)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                throw new HttpException(404, "This role was not found in the system.");
            }

            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                UsersCount = role.UserRoles.Count()
            };
        }
    }
}
