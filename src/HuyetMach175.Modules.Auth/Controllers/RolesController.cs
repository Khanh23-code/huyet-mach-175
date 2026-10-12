using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "SYS")]
    public class RolesController : ControllerBase
    {
        private readonly IRolesService _rolesService;
        
        public RolesController(IRolesService rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRolesAsync()
        {
            var roles = await _rolesService.GetRolesAsync();
            return Ok(roles);
        }

        [HttpGet("{roleId}")]
        public async Task<IActionResult> GetRoleByIdAsync([FromRoute] int roleId)
        {
            var role = await _rolesService.GetRoleByIdAsync(roleId);
            return Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoleAsync([FromBody] CreateRoleRequest request)
        {
            var newRole = await _rolesService.CreateRoleAsync(request);
            return Ok(newRole);
        }
    }
}
