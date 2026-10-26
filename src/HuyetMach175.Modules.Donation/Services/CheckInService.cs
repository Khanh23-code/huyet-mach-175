using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Appointments;
using HuyetMach175.Modules.Donation.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class CheckInService : ICheckInService
{
    private readonly ApplicationDbContext _context;
    private readonly IAppointmentValidator _validator;

    public CheckInService(ApplicationDbContext context, IAppointmentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<AppointmentDetailDto> CheckInAsync(CheckInAppointmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QrCodeToken))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã token QR không được để trống.");
        }

        var token = request.QrCodeToken.Trim();
        var appointment = await _context.DonationAppointments
            .FirstOrDefaultAsync(a => a.QrCodeToken == token);

        if (appointment == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy lịch hẹn tương ứng với mã QR này.");
        }

        if (appointment.Status == AppointmentStatus.CHECKED_IN)
        {
            var checkInTime = appointment.CheckedInAt.HasValue
                ? appointment.CheckedInAt.Value.ToLocalTime().ToString("HH:mm dd/MM/yyyy")
                : "trước đó";
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Lịch hẹn này đã được check-in vào lúc {checkInTime}.");
        }

        if (appointment.Status == AppointmentStatus.CANCELLED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lịch hẹn này đã bị hủy, không thể tiếp nhận check-in.");
        }

        if (appointment.Status == AppointmentStatus.NO_SHOW)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lịch hẹn này đã bị đánh dấu vắng mặt (NO_SHOW), không thể tiếp nhận check-in.");
        }

        if (appointment.Status != AppointmentStatus.BOOKED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Trạng thái lịch hẹn không hợp lệ để check-in (Trạng thái hiện tại: {appointment.Status}).");
        }

        appointment.Status = AppointmentStatus.CHECKED_IN;
        appointment.CheckedInAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await _context.DonationAppointments
            .AsNoTracking()
            .Where(a => a.AppointmentId == appointment.AppointmentId)
            .Select(AppointmentMappings.ProjectToDetail)
            .FirstAsync();
    }

    public async Task<AppointmentDetailDto> CreateWalkInAsync(CreateWalkInAppointmentRequest request)
    {
        var donor = await _validator.ValidateDonorAsync(request.DonorId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        await _validator.ValidateCampaignAsync(request.CampaignId, today);
        _validator.ValidateEligibility(donor, today);

        var existingBooked = await _context.DonationAppointments
            .FirstOrDefaultAsync(a => a.DonorId == request.DonorId &&
                                       a.AppointmentDate == today &&
                                       a.Status == AppointmentStatus.BOOKED);
        if (existingBooked != null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Người hiến đã có lịch hẹn đặt trước trong ngày hôm nay (Mã lịch hẹn: #{existingBooked.AppointmentId}, khung giờ: {existingBooked.TimeSlot}). Vui lòng thực hiện Check-in thay vì tạo tiếp nhận vãng lai.");
        }

        var alreadyCheckedIn = await _context.DonationAppointments
            .AnyAsync(a => a.DonorId == request.DonorId &&
                           a.AppointmentDate == today &&
                           a.Status == AppointmentStatus.CHECKED_IN);
        if (alreadyCheckedIn)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Người hiến này đã được tiếp nhận (CHECKED_IN) trong ngày hôm nay.");
        }

        string slot;
        if (!string.IsNullOrWhiteSpace(request.TimeSlot))
        {
            slot = request.TimeSlot.Trim();
        }
        else
        {
            var now = DateTime.Now;
            slot = $"{now:HH}:00-{(now.Hour + 1) % 24:D2}:00";
        }

        var nowUtc = DateTime.UtcNow;
        var appointment = new DonationAppointment
        {
            DonorId = request.DonorId,
            CampaignId = request.CampaignId,
            AppointmentDate = today,
            TimeSlot = slot,
            QrCodeToken = $"HM175-{Guid.NewGuid():N}",
            Status = AppointmentStatus.CHECKED_IN,
            IsWalkIn = true,
            CheckedInAt = nowUtc,
            CreatedAt = nowUtc
        };

        _context.DonationAppointments.Add(appointment);
        await _context.SaveChangesAsync();

        return await _context.DonationAppointments
            .AsNoTracking()
            .Where(a => a.AppointmentId == appointment.AppointmentId)
            .Select(AppointmentMappings.ProjectToDetail)
            .FirstAsync();
    }
}

