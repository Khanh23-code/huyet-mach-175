using HuyetMach175.Modules.Auth.DTOs.Users;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> GetUsersAsync([FromQuery] UserFilterDto filter)
        {
            var result = await _usersService.GetUsersAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> GetUserByIdAsync([FromRoute] int id)
        {
            var user = await _usersService.GetUserByIdAsync(id);
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
        {
            var user = await _usersService.CreateUserAsync(request);
            return Ok(user);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> UpdateUserAsync([FromRoute] int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _usersService.UpdateUserAsync(id, request);
            return Ok(user);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> UpdateUserStatusAsync([FromRoute] int id, [FromBody] UpdateUserStatusRequest request)
        {
            var user = await _usersService.UpdateUserStatusAsync(id, request);
            return Ok(user);
        }

        [HttpPut("{id}/roles")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> UpdateUserRolesAsync([FromRoute] int id, [FromBody] UpdateUserRolesRequest request)
        {
            var updatedUser = await _usersService.UpdateUserRolesAsync(id, request);
            return Ok(updatedUser);
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPasswordAsync([FromRoute] int id)
        {
            await _usersService.ResetPasswordAsync(id);
            return Ok(new { message = "Reset this user's password successfully." });
        }
    }
}
