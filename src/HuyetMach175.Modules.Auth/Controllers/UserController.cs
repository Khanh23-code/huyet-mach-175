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
            try
            {
                var result = await _authService.GetUsersAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
        {
            try
            {
                var user = await _authService.CreateUserAsync(request);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "SYS,MGT")]
        public async Task<IActionResult> UpdateUserStatusAsync([FromRoute] int id, [FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                var user = await _authService.UpdateUserStatusAsync(id, request);
                return Ok(user);
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
