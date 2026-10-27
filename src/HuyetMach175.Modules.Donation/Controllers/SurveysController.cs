using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Surveys;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.Controllers;

[ApiController]
[Route("api/surveys")]
[Authorize]
[Tags("Donation.Surveys")]
public class SurveysController : ControllerBase
{
    private readonly ISurveysService _surveysService;

    public SurveysController(ISurveysService surveysService)
    {
        _surveysService = surveysService;
    }

    [HttpGet("queue")]
    [HasPermission("SCREENING_VIEW")]
    [EndpointSummary("Lấy danh sách hàng chờ khám sàng lọc trong ngày")]
    [EndpointDescription(
        "Lấy danh sách các lịch hẹn đã tiếp nhận (`CHECKED_IN`) trong ngày kèm trạng thái khám sàng lọc tương ứng để hiển thị cột hàng chờ trên giao diện Bác sĩ.\n\n" +
        "**Quy tắc nghiệp vụ:**\n" +
        "- Nếu không truyền `date`, hệ thống mặc định lấy ngày hiện tại.\n" +
        "- Khi lọc `screeningStatus = PENDING`, hệ thống bao gồm cả các lịch hẹn đã `CHECKED_IN` nhưng chưa điền bảng hỏi trực tuyến (ví dụ khách vãng lai Walk-in) và các lịch hẹn đã điền bảng hỏi đang chờ Bác sĩ khám.\n" +
        "- Danh sách được sắp xếp theo thứ tự thời gian Check-in tăng dần (FIFO - người đến trước được khám trước).\n" +
        "- Hỗ trợ tìm kiếm nhanh (`search`) theo họ tên, CCCD, số điện thoại hoặc mã QR.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **SCREENING_VIEW** (các vai trò như CLIN, DON, MGT, BBNK).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về danh sách hàng chờ khám sàng lọc phân trang.\n" +
        "- 400 BadRequest: Tham số phân trang không hợp lệ.\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập SCREENING_VIEW.")]
    [ProducesResponseType(typeof(PagedResult<SurveyQueueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetScreeningQueue([FromQuery] SurveyQueueFilterDto filter)
    {
        var result = await _surveysService.GetScreeningQueueAsync(filter);
        return Ok(result);
    }

    [HttpGet("appointment/{appointmentId:int}")]
    [HasPermission("SURVEY_VIEW")]
    [EndpointSummary("Xem chi tiết hồ sơ sàng lọc và bảng hỏi theo mã lịch hẹn")]
    [EndpointDescription(
        "Trả về toàn bộ hồ sơ sàng lọc khi Bác sĩ bấm vào một người hiến trong hàng chờ, bao gồm:\n" +
        "- **Thông tin tóm tắt Người hiến (`donor`):** Họ tên, mã người hiến (`donorCode`), CCCD, ngày sinh, tuổi, giới tính, nhóm máu, số lần hiến, ngày hiến gần nhất và trạng thái đủ 84 ngày (`isEligible84Days`).\n" +
        "- **Phần 1 (Bảng hỏi trực tuyến):** Nội dung câu trả lời JSON (`surveyAnswers`), điểm nguy cơ (`riskScore`), cờ cảnh báo (`hasRisk`), thời điểm nộp (`surveySubmittedAt`).\n" +
        "- **Phần 2 (Chỉ số sinh tồn & lâm sàng):** Cân nặng, huyết áp tâm thu/tâm trương, mạch, nồng độ Hemoglobin, thân nhiệt, kết quả test nhanh HBsAg và ghi chú lâm sàng.\n" +
        "- **Phần 3 (Kết luận sàng lọc):** Bác sĩ khám, trạng thái sàng lọc (`PENDING`, `QUALIFIED`, `DEFERRED`, `UNQUALIFIED`), lý do tạm hoãn/từ chối, thể tích máu chỉ định (`targetVolumeMl`) và thời điểm khám.\n\n" +
        "**Lưu ý:** Nếu người hiến đã `CHECKED_IN` nhưng chưa khai bảng hỏi trực tuyến (chưa có bản ghi trong bảng `pre_screening_surveys`), API vẫn trả về `200 OK` với thông tin người hiến và trạng thái sàng lọc mặc định `PENDING` để Bác sĩ có thể nhập trực tiếp trên form khám.\n\n" +
        "**Yêu cầu quyền truy cập:** Bearer Token với quyền **SURVEY_VIEW** (các vai trò như CLIN, DON, MGT).\n\n" +
        "**HTTP Response:**\n" +
        "- 200 OK: Trả về chi tiết hồ sơ sàng lọc của lịch hẹn.\n" +
        "- 400 BadRequest: ID lịch hẹn không hợp lệ (<= 0).\n" +
        "- 401 Unauthorized: Chưa đăng nhập hoặc token không hợp lệ.\n" +
        "- 403 Forbidden: Không có quyền truy cập SURVEY_VIEW.\n" +
        "- 404 NotFound: Không tìm thấy lịch hẹn với ID chỉ định.")]
    [ProducesResponseType(typeof(SurveyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSurveyByAppointmentId([FromRoute] int appointmentId)
    {
        var result = await _surveysService.GetSurveyByAppointmentIdAsync(appointmentId);
        return Ok(result);
    }
}