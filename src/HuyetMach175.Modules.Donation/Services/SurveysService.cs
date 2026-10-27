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

        int riskScore = 0;
        foreach (var property in request.SurveyAnswers.EnumerateObject())
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

        bool hasRisk = riskScore > 0;
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
}