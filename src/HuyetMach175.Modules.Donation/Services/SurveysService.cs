using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Surveys;
using HuyetMach175.Modules.Donation.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class SurveysService : ISurveysService
{
    private readonly ApplicationDbContext _context;

    public SurveysService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SurveyQueueItemDto>> GetScreeningQueueAsync(SurveyQueueFilterDto filter)
    {
        if (filter.Page < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var targetDate = filter.Date ?? DateOnly.FromDateTime(DateTime.Today);

        var query = _context.DonationAppointments
            .AsNoTracking()
            .Include(a => a.Donor)
            .Include(a => a.CampaignSlot)
                .ThenInclude(s => s!.Campaign)
            .Include(a => a.PreScreeningSurvey)
            .Where(a => a.Status == AppointmentStatus.CHECKED_IN && a.AppointmentDate == targetDate);

        if (filter.CampaignId.HasValue)
        {
            query = query.Where(a => a.CampaignSlot != null && a.CampaignSlot.CampaignId == filter.CampaignId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ScreeningStatus))
        {
            var statusStr = filter.ScreeningStatus.Trim().ToUpperInvariant();
            if (Enum.TryParse<ScreeningStatus>(statusStr, true, out var status))
            {
                if (status == ScreeningStatus.PENDING)
                {
                    query = query.Where(a =>
                        a.PreScreeningSurvey == null ||
                        a.PreScreeningSurvey.ScreeningStatus == ScreeningStatus.PENDING);
                }
                else
                {
                    query = query.Where(a =>
                        a.PreScreeningSurvey != null &&
                        a.PreScreeningSurvey.ScreeningStatus == status);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(a =>
                (a.Donor != null && (
                    EF.Functions.ILike(a.Donor.FullName, $"%{kw}%") ||
                    EF.Functions.ILike(a.Donor.CitizenId, $"%{kw}%") ||
                    EF.Functions.ILike(a.Donor.PhoneNumber, $"%{kw}%")
                )) ||
                EF.Functions.ILike(a.QrCodeToken, $"%{kw}%"));
        }

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(a => a.CheckedInAt ?? a.CreatedAt)
            .ThenBy(a => a.AppointmentId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = entities.Select(a => a.ToQueueItemDto()).ToList();

        return new PagedResult<SurveyQueueItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<SurveyDetailDto> GetSurveyByAppointmentIdAsync(int appointmentId)
    {
        if (appointmentId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID lịch hẹn không hợp lệ.");
        }

        var appointment = await _context.DonationAppointments
            .AsNoTracking()
            .Include(a => a.Donor)
            .Include(a => a.CampaignSlot)
                .ThenInclude(s => s!.Campaign)
            .Include(a => a.PreScreeningSurvey)
                .ThenInclude(s => s!.Doctor)
            .Include(a => a.DonationSession)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lịch hẹn với ID {appointmentId}.");
        }

        return appointment.ToSurveyDetailDto();
    }

    public async Task<SubmitOnlineSurveyResponse> SubmitOnlineSurveyAsync(SubmitOnlineSurveyRequest request)
    {
        if (request.AppointmentId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID lịch hẹn không hợp lệ.");
        }

        if (request.SurveyAnswers.ValueKind != JsonValueKind.Object)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Dữ liệu câu trả lời khảo sát phải là một JSON object hợp lệ.");
        }

        var appointment = await _context.DonationAppointments
            .Include(a => a.PreScreeningSurvey)
            .FirstOrDefaultAsync(a => a.AppointmentId == request.AppointmentId);

        if (appointment == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lịch hẹn với ID {request.AppointmentId}.");
        }

        if (appointment.Status == AppointmentStatus.CANCELLED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lịch hẹn này đã bị hủy, không thể nộp bảng khảo sát.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (appointment.AppointmentDate < today)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lịch hẹn đã quá hạn khám, không thể nộp bảng khảo sát.");
        }

        if (appointment.PreScreeningSurvey != null &&
            appointment.PreScreeningSurvey.ScreeningStatus != ScreeningStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lịch hẹn này đã hoàn tất khám sàng lọc, không thể nộp lại bảng khảo sát trực tuyến.");
        }

        var (riskScore, hasRisk) = CalculateSurveyRisk(request.SurveyAnswers);
        var now = DateTime.UtcNow;

        PreScreeningSurvey survey;
        if (appointment.PreScreeningSurvey == null)
        {
            survey = new PreScreeningSurvey
            {
                AppointmentId = appointment.AppointmentId,
                SurveyAnswers = request.SurveyAnswers.GetRawText(),
                RiskScore = riskScore,
                HasRisk = hasRisk,
                SurveySubmittedAt = now,
                ScreeningStatus = ScreeningStatus.PENDING
            };
            _context.PreScreeningSurveys.Add(survey);
        }
        else
        {
            survey = appointment.PreScreeningSurvey;
            survey.SurveyAnswers = request.SurveyAnswers.GetRawText();
            survey.RiskScore = riskScore;
            survey.HasRisk = hasRisk;
            survey.SurveySubmittedAt = now;
            survey.ScreeningStatus = ScreeningStatus.PENDING;
        }

        await _context.SaveChangesAsync();

        return new SubmitOnlineSurveyResponse
        {
            SurveyId = survey.SurveyId,
            AppointmentId = appointment.AppointmentId,
            HasRisk = hasRisk,
            RiskScore = riskScore,
            SubmittedAt = now,
            Message = "Nộp bảng khảo sát sức khỏe thành công"
        };
    }

    public async Task<ClinicalScreeningResponse> UpdateClinicalScreeningAsync(
        int appointmentId,
        UpdateClinicalScreeningRequest request,
        int doctorId)
    {
        if (appointmentId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID lịch hẹn không hợp lệ.");
        }

        if (doctorId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Không tìm thấy thông tin Bác sĩ thực hiện khám.");
        }

        if (request.ScreeningStatus == ScreeningStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kết luận sàng lọc phải là Đạt tiêu chuẩn (QUALIFIED), Tạm hoãn (DEFERRED) hoặc Không đạt (UNQUALIFIED).");
        }

        if (request.ScreeningStatus == ScreeningStatus.QUALIFIED)
        {
            if (request.RapidHbsAgNegative != true)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Người hiến có kết quả xét nghiệm nhanh HBsAg dương tính hoặc chưa có kết quả, không đủ điều kiện hiến máu.");
            }

            if (!request.TargetVolumeMl.HasValue)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Bắt buộc phải chỉ định lượng máu hiến (targetVolumeMl) khi kết luận Đạt tiêu chuẩn.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.DeferralReason))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Bắt buộc phải nhập lý do (deferralReason) khi kết luận Tạm hoãn hoặc Không đạt.");
            }
        }

        var appointment = await _context.DonationAppointments
            .Include(a => a.PreScreeningSurvey)
            .Include(a => a.DonationSession)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lịch hẹn với ID {appointmentId}.");
        }

        if (appointment.Status == AppointmentStatus.CANCELLED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lịch hẹn này đã bị hủy, không thể thực hiện khám sàng lọc.");
        }

        if (appointment.Status != AppointmentStatus.CHECKED_IN)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Người hiến chưa hoàn tất thủ tục tiếp nhận (Check-in), chưa thể khám sàng lọc.");
        }

        if (appointment.DonationSession != null &&
            appointment.DonationSession.Status == SessionStatus.COMPLETED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiên hiến máu đã hoàn thành, không thể sửa kết luận khám sàng lọc.");
        }

        var now = DateTime.UtcNow;

        if (appointment.PreScreeningSurvey == null)
        {
            appointment.PreScreeningSurvey = new PreScreeningSurvey
            {
                AppointmentId = appointment.AppointmentId,
                SurveyAnswers = "{}",
                ScreeningStatus = ScreeningStatus.PENDING
            };
            _context.PreScreeningSurveys.Add(appointment.PreScreeningSurvey);
        }

        var survey = appointment.PreScreeningSurvey;

        if (request.SurveyAnswers.HasValue && request.SurveyAnswers.Value.ValueKind == JsonValueKind.Object)
        {
            var (riskScore, hasRisk) = CalculateSurveyRisk(request.SurveyAnswers.Value);

            survey.SurveyAnswers = request.SurveyAnswers.Value.GetRawText();
            survey.RiskScore = riskScore;
            survey.HasRisk = hasRisk;
            survey.SurveySubmittedAt ??= now;
        }
        else if (string.IsNullOrWhiteSpace(survey.SurveyAnswers) || survey.SurveyAnswers == "{}")
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Người hiến chưa có thông tin bảng hỏi tiền sử sức khỏe. Vui lòng gửi kèm surveyAnswers đối với khách vãng lai.");
        }

        survey.DoctorId = doctorId;
        survey.WeightKg = request.WeightKg;
        survey.SystolicBloodPressure = request.SystolicBloodPressure;
        survey.DiastolicBloodPressure = request.DiastolicBloodPressure;
        survey.Pulse = request.Pulse;
        survey.HemoglobinLevel = request.HemoglobinLevel;
        survey.BodyTemp = request.BodyTemp;
        survey.RapidHbsAgNegative = request.RapidHbsAgNegative;
        survey.ClinicalNotes = request.ClinicalNotes;
        survey.ScreeningStatus = request.ScreeningStatus;
        survey.DeferralReason = request.ScreeningStatus == ScreeningStatus.QUALIFIED ? null : request.DeferralReason;
        survey.TargetVolumeMl = request.ScreeningStatus == ScreeningStatus.QUALIFIED ? request.TargetVolumeMl : null;
        survey.ScreenedAt = now;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        DonationSession? session = appointment.DonationSession;

        if (request.ScreeningStatus == ScreeningStatus.QUALIFIED)
        {
            if (session == null)
            {
                session = new DonationSession
                {
                    AppointmentId = appointment.AppointmentId,
                    TargetVolumeMl = request.TargetVolumeMl!.Value,
                    Status = SessionStatus.WAITING_COLLECTION,
                    CreatedAt = now
                };
                _context.DonationSessions.Add(session);
            }
            else
            {
                session.TargetVolumeMl = request.TargetVolumeMl!.Value;
                session.Status = SessionStatus.WAITING_COLLECTION;
            }
        }
        else
        {
            if (session != null && session.Status == SessionStatus.WAITING_COLLECTION)
            {
                _context.DonationSessions.Remove(session);
                session = null;
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        string message = request.ScreeningStatus switch
        {
            ScreeningStatus.QUALIFIED => "Đã lưu kết quả khám. Người hiến đủ điều kiện và đã chuyển sang phòng lấy máu.",
            ScreeningStatus.DEFERRED => "Đã lưu kết quả khám. Người hiến thuộc diện tạm hoãn hiến máu.",
            ScreeningStatus.UNQUALIFIED => "Đã lưu kết quả khám. Người hiến không đạt tiêu chuẩn hiến máu.",
            _ => "Đã lưu kết quả khám sàng lọc thành công."
        };

        return new ClinicalScreeningResponse
        {
            AppointmentId = appointment.AppointmentId,
            ScreeningStatus = request.ScreeningStatus.ToString(),
            SessionId = session?.SessionId,
            SessionStatus = session != null ? session.Status.ToString() : null,
            TargetVolumeMl = request.ScreeningStatus == ScreeningStatus.QUALIFIED ? request.TargetVolumeMl?.ToString() : null,
            ScreenedAt = now,
            Message = message
        };
    }

    private static (int RiskScore, bool HasRisk) CalculateSurveyRisk(JsonElement answers)
    {
        int riskScore = 0;
        foreach (var property in answers.EnumerateObject())
        {
            if (string.Equals(property.Name, "q1_previous_donation", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.True ||
                (property.Value.ValueKind == JsonValueKind.String &&
                 bool.TryParse(property.Value.GetString(), out var boolVal) && boolVal))
            {
                riskScore++;
            }
        }

        return (riskScore, riskScore > 0);
    }
}