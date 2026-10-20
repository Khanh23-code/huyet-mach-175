using HuyetMach175.Modules.Auth.DTOs.Departments;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/departments")]
    [Authorize(Roles = "SYS,MGT")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public DepartmentsController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsAsync()
        {
            var departments = await _usersService.GetDepartmentsAsync();
            return Ok(departments);
        }
    }
}

