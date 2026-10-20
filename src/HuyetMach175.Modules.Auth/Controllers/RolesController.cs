using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.Modules.Auth.DTOs.Roles;
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
        [Authorize(Roles = "SYS,MGT")]
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

        [HttpPut("{roleId}")]
        public async Task<IActionResult> UpdateRoleAsync([FromRoute] int roleId, [FromBody] UpdateRoleRequest request)
        {
            var updatedRole = await _rolesService.UpdateRoleAsync(roleId, request);
            return Ok(updatedRole);
        }

        [HttpPatch("{roleId}/status")]
        public async Task<IActionResult> UpdateRoleStatusAsync(
            [FromRoute] int roleId,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] UpdateRoleStatusRequest? request = null,
            [FromQuery] bool force = false)
        {
            var updatedRole = await _rolesService.UpdateRoleStatusAsync(roleId, request, force);
            return Ok(updatedRole);
        }

        [HttpGet("{roleId}/permissions")]
        public async Task<IActionResult> GetRolePermissionsAsync([FromRoute] int roleId)
        {
            var result = await _rolesService.GetRolePermissionsAsync(roleId);
            return Ok(result);
        }

        [HttpPut("{roleId}/permissions")]
        public async Task<IActionResult> UpdateRolePermissionsAsync([FromRoute] int roleId, [FromBody] UpdateRolePermissionsRequest request)
        {
            var result = await _rolesService.UpdateRolePermissionsAsync(roleId, request);
            return Ok(result);
        }
    }
}