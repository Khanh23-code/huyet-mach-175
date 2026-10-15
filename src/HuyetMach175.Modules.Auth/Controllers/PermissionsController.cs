using HuyetMach175.Modules.Auth.DTOs.Permissions;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    [Authorize(Roles = "SYS")]
    public class PermissionsController : ControllerBase
    {
        private readonly IRolesService _rolesService;

        public PermissionsController(IRolesService rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPermissionsAsync()
        {
            var permissions = await _rolesService.GetAllPermissionsAsync();
            return Ok(permissions);
        }
    }
}
