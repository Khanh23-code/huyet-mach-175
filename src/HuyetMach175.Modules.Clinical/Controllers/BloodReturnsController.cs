using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Clinical.Controllers;

[ApiController]
[Route("api/blood-returns")]
[Authorize]
[Tags("Clinical.BloodReturns")]
public class BloodReturnsController : ControllerBase
{
    private readonly IBloodReturnsService _bloodReturnsService;

    public BloodReturnsController(IBloodReturnsService bloodReturnsService)
    {
        _bloodReturnsService = bloodReturnsService;
    }

    [HttpPost]
    [HasPermission("BLOOD_RETURN_EDIT")]
    [EndpointSummary("Lập phiếu hoàn trả túi máu chưa sử dụng")]
    [EndpointDescription(
        "Lâm sàng lập phiếu hoàn trả túi máu đã xuất kho nhưng không sử dụng và phong tỏa ngay túi máu về trạng thái `LOCKED` trong lúc vận chuyển về Ngân hàng máu.\n\n" +
        "**Quy tắc nghiệp vụ (Transaction):**\n" +
        "- Bắt buộc đạt cả 4 điều kiện bảng kiểm (`isUnusedFullVolume`, `isBagIntact`, `isLabelClear`, `isStoredUnder30Mins`).\n" +
        "- Chỉ người dùng thuộc cùng khoa với phiếu yêu cầu mới được lập phiếu hoàn trả; mỗi lượt phân bổ chỉ được hoàn trả tối đa 1 lần.\n" +
        "- Tạo bản ghi `blood_returns` với `qualified = false`, `final_action = PENDING_INSPECTION`.\n" +
        "- Chuyển trạng thái túi máu sang `LOCKED` và ghi vết `audit_logs`, `blood_bag_status_logs` (`ISSUED` -> `LOCKED`).\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **BLOOD_RETURN_EDIT** (vai trò CLIN).\n\n" +
        "**HTTP Response:**\n" +
        "- 201 Created: Lập phiếu hoàn trả thành công.\n" +
        "- 400 BadRequest: Bảng kiểm không đạt đủ 4 điều kiện, thiếu lý do hoặc trạng thái túi không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền BLOOD_RETURN_EDIT hoặc khác khoa điều trị.\n" +
        "- 404 NotFound: Không tìm thấy lượt phân bổ.\n" +
        "- 409 Conflict: Lượt phân bổ này đã có phiếu hoàn trả trước đó.")]
    [ProducesResponseType(typeof(CreateBloodReturnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBloodReturn([FromBody] CreateBloodReturnRequest request)
    {
        var userId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _bloodReturnsService.CreateBloodReturnAsync(request, userId, ipAddress);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
