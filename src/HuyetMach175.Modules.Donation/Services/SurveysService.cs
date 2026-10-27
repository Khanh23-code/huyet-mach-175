using HuyetMach175.Api.Data;
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
            .Where(a => a.Status == AppointmentStatus.CHECKED_IN && a.AppointmentDate == targetDate);

        if (filter.CampaignId.HasValue)
        {
            query = query.Where(a => a.CampaignId == filter.CampaignId.Value);
        }

        if (filter.ScreeningStatus.HasValue)
        {
            var status = filter.ScreeningStatus.Value;
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

        var items = await query
            .OrderBy(a => a.CheckedInAt ?? a.CreatedAt)
            .ThenBy(a => a.AppointmentId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(SurveyMappings.ProjectToQueueItem)
            .ToListAsync();

        foreach (var item in items)
        {
            item.DonorCode = $"NH{item.DonorId:D5}";
        }

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
            .Include(a => a.Campaign)
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
}