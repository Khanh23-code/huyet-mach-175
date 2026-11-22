using HuyetMach175.Modules.Auth.DTOs.Departments;
using HuyetMach175.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/departments")]
    [Authorize(Roles = "SYS,MGT")]
    [Tags("Identity.Departments")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentsService _departmentsService;

        public DepartmentsController(IDepartmentsService departmentsService)
        {
            _departmentsService = departmentsService;
        }

        [HttpGet]
        [EndpointSummary("Lấy danh sách khoa / phòng ban")]
        [EndpointDescription(
            "Trả về danh sách tất cả các khoa/phòng ban trong bệnh viện (bao gồm ID, mã khoa phòng và tên hiển thị).\n\n" +
            "Thường dùng để đổ dữ liệu vào dropdown chọn khoa/phòng trong các form quản lý tài khoản người dùng.\n\n" +
            "**Yêu cầu quyền truy cập:** Bearer Token của người dùng có vai trò **SYS** (Quản trị hệ thống) hoặc **MGT** (Ban Giám đốc / Quản lý).\n\n" + 
            "**HTTP Response:**" + 
            "\n- 200 OK: Trả về danh sách các khoa/phòng ban." +
            "\n- 401 Unauthorized: Người dùng chưa xác thực hoặc token không hợp lệ." +
            "\n- 403 Forbidden: Người dùng đã xác thực nhưng không có quyền truy cập.")]
        [ProducesResponseType(typeof(List<DepartmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartmentsAsync()
        {
            var departments = await _departmentsService.GetDepartmentsAsync();
            return Ok(departments);
        }
    }
}
