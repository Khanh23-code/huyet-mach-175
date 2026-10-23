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

    [HttpGet("check-eligibility")]
    [HasPermission("DONORS_VIEW")]
    [EndpointSummary("Kiểm tra nhanh điều kiện hiến máu theo số CCCD")]
    [EndpointDescription(
        "Kiểm tra nhanh người hiến máu có đủ điều kiện về khoảng cách ngày (tối thiểu 84 ngày đối với máu toàn phần) " +
        "và độ tuổi (18 - 60 tuổi theo Thông tư 26/2013/TT-BYT) hay không trước khi tiếp nhận.\n\n" +
        "**Xử lý các kịch bản nghiệp vụ:**\n" +
        "- Nếu số CCCD chưa có trong hệ thống: Trả về trạng thái đủ điều kiện (isEligible = true) với thông báo người hiến mới, không báo lỗi 404 để bàn tiếp đón xử lý đăng ký.\n" +
        "- Nếu số CCCD đã tồn tại: Tự động trả về thông tin người hiến và tính toán khoảng cách so với lần hiến gần nhất (lastDonationDate). Nếu dưới 84 ngày, trả về cảnh báo kèm số ngày còn lại (daysRemaining) và ngày sớm nhất có thể hiến (nextEligibleDate).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DONORS_VIEW** (các vai trò như DON, CLIN, MGT, BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về kết quả kiểm tra điều kiện hiến máu kèm thông tin người hiến (nếu có).\n" +
        "- 400 BadRequest: Số CCCD không hợp lệ (trống hoặc không đúng định dạng 12 chữ số).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DONORS_VIEW.")]
    [ProducesResponseType(typeof(DonorEligibilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckEligibility([FromQuery] string citizenId)
    {
        var result = await _donorsService.CheckEligibilityAsync(citizenId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission("DONORS_VIEW")]
    [EndpointSummary("Xem chi tiết hồ sơ người hiến máu và lịch sử hiến")]
    [EndpointDescription(
        "Trả về thông tin chi tiết của người hiến máu theo mã định danh (donorId), bao gồm nhóm máu, " +
        "tổng số lần đã hiến (totalDonations), ngày hiến gần nhất và toàn bộ danh sách lịch sử các lần đăng ký / hiến máu trước đó.\n\n" +
        "Mỗi lượt hiến trong lịch sử hiển thị ngày hẹn, đợt hiến máu (hoặc tại viện), thể tích thực tế (ml), " +
        "kết quả khám sàng lọc và trạng thái hoàn thành.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DONORS_VIEW** (các vai trò như DON, CLIN, MGT, BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết hồ sơ người hiến máu kèm lịch sử hiến.\n" +
        "- 400 BadRequest: ID người hiến máu không hợp lệ (id <= 0).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DONORS_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy người hiến máu với ID chỉ định.")]
    [ProducesResponseType(typeof(DonorDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDonorById(int id)
    {
        var result = await _donorsService.GetDonorByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission("DONORS_EDIT")]
    [EndpointSummary("Đăng ký mới hồ sơ người hiến máu")]
    [EndpointDescription(
        "Đăng ký hồ sơ người hiến máu mới tại quầy tiếp nhận (áp dụng cho người hiến vãng lai lần đầu đến viện).\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Số CCCD gồm đúng 12 chữ số và không được trùng với người hiến khác trong hệ thống (nếu trùng trả về 409 Conflict).\n" +
        "- Số điện thoại gồm 10 chữ số bắt đầu bằng 0 và không được trùng với người hiến khác (nếu trùng trả về 409 Conflict).\n" +
        "- Người hiến máu phải từ đủ 18 tuổi đến 60 tuổi theo quy định y tế.\n" +
        "- Nhóm máu (bloodType) và yếu tố Rh (rhFactor) ban đầu được để trống (null), sẽ được cập nhật tự động sau quy trình xét nghiệm lâm sàng.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **DONORS_EDIT** (các vai trò như DON, CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Đăng ký hồ sơ thành công, trả về chi tiết hồ sơ vừa tạo.\n" +
        "- 400 BadRequest: Dữ liệu không hợp lệ hoặc độ tuổi ngoài khoảng 18 - 60.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập DONORS_EDIT.\n" +
        "- 409 Conflict: Số CCCD hoặc Số điện thoại đã tồn tại trong hệ thống.")]
    [ProducesResponseType(typeof(DonorDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDonor([FromBody] CreateDonorRequest request)
    {
        var result = await _donorsService.CreateDonorAsync(request);
        return CreatedAtAction(nameof(GetDonorById), new { id = result.DonorId }, result);
    }
}