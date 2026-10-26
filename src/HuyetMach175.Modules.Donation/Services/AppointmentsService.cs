using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Appointments;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly ApplicationDbContext _context;
    private readonly IAppointmentValidator _validator;

    public AppointmentsService(ApplicationDbContext context, IAppointmentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<PagedResult<AppointmentListItemDto>> GetAppointmentsAsync(AppointmentFilterDto filter)
    {
        if (filter.Page < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.DonationAppointments
            .AsNoTracking();

        if (filter.Date.HasValue)
        {
            query = query.Where(a => a.AppointmentDate == filter.Date.Value);
        }

        if (filter.CampaignId.HasValue)
        {
            query = query.Where(a => a.CampaignId == filter.CampaignId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TimeSlot))
        {
            var slot = filter.TimeSlot.Trim();
            query = query.Where(a => a.TimeSlot == slot);
        }

        if (filter.IsWalkIn.HasValue)
        {
            query = query.Where(a => a.IsWalkIn == filter.IsWalkIn.Value);
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
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.TimeSlot)
            .ThenBy(a => a.AppointmentId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(AppointmentMappings.ProjectToListItem)
            .ToListAsync();

        return new PagedResult<AppointmentListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<AppointmentDetailDto> GetAppointmentByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID lịch hẹn không hợp lệ.");
        }

        var appointment = await _context.DonationAppointments
            .AsNoTracking()
            .Where(a => a.AppointmentId == id)
            .Select(AppointmentMappings.ProjectToDetail)
            .FirstOrDefaultAsync();

        if (appointment == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lịch hẹn với ID {id}.");
        }

        return appointment;
    }

    public async Task<AppointmentDetailDto> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        var donor = await _validator.ValidateDonorAsync(request.DonorId);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.AppointmentDate < today)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Ngày hẹn không được nằm trong quá khứ.");
        }

        await _validator.ValidateCampaignAsync(request.CampaignId, request.AppointmentDate);
        _validator.ValidateEligibility(donor, request.AppointmentDate);
        await _validator.ValidateNoDuplicateBookingAsync(request.DonorId, request.AppointmentDate);

        var qrToken = $"HM175-{Guid.NewGuid():N}";

        var appointment = new DonationAppointment
        {
            DonorId = request.DonorId,
            CampaignId = request.CampaignId,
            AppointmentDate = request.AppointmentDate,
            TimeSlot = request.TimeSlot.Trim(),
            QrCodeToken = qrToken,
            Status = AppointmentStatus.BOOKED,
            IsWalkIn = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.DonationAppointments.Add(appointment);
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointment.AppointmentId);
    }

    public async Task<AppointmentDetailDto> CancelAppointmentAsync(int id, CancelAppointmentRequest? request)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID lịch hẹn không hợp lệ.");
        }

        var appointment = await _context.DonationAppointments
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appointment == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lịch hẹn với ID {id}.");
        }

        if (appointment.Status == AppointmentStatus.CHECKED_IN)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Lịch hẹn này đã được tiếp nhận (CHECKED_IN), không thể hủy.");
        }

        if (appointment.Status == AppointmentStatus.CANCELLED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lịch hẹn này đã bị hủy trước đó.");
        }

        var targetStatus = request?.Status == AppointmentStatus.NO_SHOW
            ? AppointmentStatus.NO_SHOW
            : AppointmentStatus.CANCELLED;

        appointment.Status = targetStatus;

        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointment.AppointmentId);
    }
}
