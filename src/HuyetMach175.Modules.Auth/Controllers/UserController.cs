using HuyetMach175.Modules.Auth.DTOs;
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
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> GetUsersAsync([FromQuery] UserFilterDto filter)
        {
            var result = await _authService.GetUsersAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> GetUserByIdAsync([FromRoute] int id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
        {
            var user = await _authService.CreateUserAsync(request);
            return Ok(user);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUserProfileAsync([FromBody] UpdateProfileRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var updatedProfile = await _authService.UpdateCurrentUserProfileAsync(userId, request);

            return Ok(updatedProfile);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> UpdateUserAsync([FromRoute] int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _authService.UpdateUserAsync(id, request);
            return Ok(user);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> UpdateUserStatusAsync([FromRoute] int id, [FromBody] UpdateUserStatusRequest request)
        {
            var user = await _authService.UpdateUserStatusAsync(id, request);
            return Ok(user);
        }

        [HttpPut("{id}/roles")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> UpdateUserRolesAsync([FromRoute] int id, [FromBody] UpdateUserRolesRequest request)
        {
            var updatedUser = await _authService.UpdateUserRolesAsync(id, request);
            return Ok(updatedUser);
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPasswordAsync([FromRoute] int id)
        {
            await _authService.ResetPasswordAsync(id);
            return Ok(new { message = "Reset this user's password successfully." });
        }
    }
}
