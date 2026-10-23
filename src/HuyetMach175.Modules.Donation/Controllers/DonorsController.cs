using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Donors;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.Controllers;

[ApiController]
[Route("api/donors")]
[Authorize]
[Tags("Donation.Donors")]
public class DonorsController : ControllerBase
{
    private readonly IDonorsService _donorsService;

    public DonorsController(IDonorsService donorsService)
    {
        _donorsService = donorsService;
    }

    [HttpGet]
    [HasPermission("DONORS_VIEW")]
    [EndpointSummary("Tìm kiếm danh sách người hiến máu")]
    [EndpointDescription(
        "Trả về danh sách người hiến máu có hỗ trợ phân trang, tìm kiếm nhanh theo số CCCD (citizenId), " +
        "họ tên (fullName) hoặc số điện thoại (phoneNumber), cùng bộ lọc tùy chọn theo nhóm máu (bloodType) và yếu tố Rh (rhFactor).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DONORS_VIEW** (các vai trò như DON, CLIN, MGT, BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách người hiến máu theo cấu trúc phân trang PagedResult.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ (page < 1, pageSize ngoài [1, 100]).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DONORS_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<DonorListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDonors([FromQuery] DonorFilterDto filter)
    {
        var result = await _donorsService.GetDonorsAsync(filter);
        return Ok(result);
    }
}