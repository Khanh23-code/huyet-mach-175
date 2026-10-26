using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class AppointmentValidator : IAppointmentValidator
{
    private readonly ApplicationDbContext _context;

    public AppointmentValidator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Donor> ValidateDonorAsync(int donorId)
    {
        if (donorId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã người hiến máu (donorId) không hợp lệ.");
        }

        var donor = await _context.Donors.FirstOrDefaultAsync(d => d.DonorId == donorId);
        if (donor == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy người hiến máu với ID {donorId}.");
        }

        return donor;
    }

    public void ValidateEligibility(Donor donor, DateOnly targetDate)
    {
        if (!donor.LastDonationDate.HasValue) return;

        var daysSinceLast = targetDate.DayNumber - donor.LastDonationDate.Value.DayNumber;
        if (daysSinceLast < 84)
        {
            var nextEligible = donor.LastDonationDate.Value.AddDays(84);
            var daysRemaining = 84 - daysSinceLast;
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Người hiến chưa đủ khoảng cách tối thiểu 84 ngày tính đến ngày {targetDate:dd/MM/yyyy}. Lần hiến gần nhất là {donor.LastDonationDate.Value:dd/MM/yyyy}. Cần chờ thêm {daysRemaining} ngày (ngày sớm nhất có thể hiến: {nextEligible:dd/MM/yyyy}).");
        }
    }

    public async Task ValidateCampaignAsync(int? campaignId, DateOnly appointmentDate, string? timeSlot = null, int? slotId = null)
    {
        if (!campaignId.HasValue) return;

        var campaign = await _context.DonationCampaigns
            .Include(c => c.Slots)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId.Value);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy chiến dịch hiến máu với ID {campaignId.Value}.");
        }

        if (campaign.Status != CampaignStatus.APPROVED && campaign.Status != CampaignStatus.IN_PROGRESS)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Chiến dịch hiến máu hiện không ở trạng thái mở tiếp nhận đăng ký (trạng thái hiện tại: {campaign.Status}).");
        }

        if (appointmentDate < campaign.StartDate || appointmentDate > campaign.EndDate)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Ngày hẹn ({appointmentDate:dd/MM/yyyy}) nằm ngoài thời gian diễn ra chiến dịch ({campaign.StartDate:dd/MM/yyyy} - {campaign.EndDate:dd/MM/yyyy}).");
        }

        // Kiểm tra ngày hoạt động trong tuần nếu là RECURRING
        if (campaign.CampaignType == CampaignType.RECURRING && !string.IsNullOrWhiteSpace(campaign.OperatingDays))
        {
            var dow = appointmentDate.DayOfWeek.ToString().ToUpperInvariant().Substring(0, 3);
            var allowedDays = campaign.OperatingDays.ToUpperInvariant().Split(',', StringSplitOptions.TrimEntries);
            if (!allowedDays.Contains(dow))
            {
                throw new HttpException(StatusCodes.Status400BadRequest,
                    $"Đợt hiến máu định kỳ chỉ mở tiếp nhận vào các ngày: {campaign.OperatingDays}. Ngày bạn chọn ({appointmentDate:dd/MM/yyyy}) không có lịch tiếp nhận.");
            }
        }

        // Kiểm tra sức chứa tối đa của cả campaign (nếu có cấu hình)
        if (campaign.MaxCapacity.HasValue && campaign.MaxCapacity.Value > 0)
        {
            var totalActiveBookings = await _context.DonationAppointments
                .CountAsync(a => a.CampaignId == campaign.CampaignId && a.Status != AppointmentStatus.CANCELLED);

            if (totalActiveBookings >= campaign.MaxCapacity.Value)
            {
                throw new HttpException(StatusCodes.Status400BadRequest,
                    $"Đợt hiến máu này đã đạt số lượng đăng ký tối đa ({campaign.MaxCapacity.Value} người). Hệ thống đã đóng tiếp nhận.");
            }
        }

        // Kiểm tra sức chứa tối đa theo từng khung giờ (Slot Capacity)
        var slot = campaign.Slots.FirstOrDefault(s =>
            (slotId.HasValue && s.SlotId == slotId.Value) ||
            (!string.IsNullOrWhiteSpace(timeSlot) && $"{s.StartTime:HH\\:mm}-{s.EndTime:HH\\:mm}" == timeSlot.Trim()));

        if (slot != null && slot.Capacity > 0)
        {
            var slotBooked = await _context.DonationAppointments.CountAsync(a =>
                a.CampaignId == campaign.CampaignId &&
                a.AppointmentDate == appointmentDate &&
                (a.SlotId == slot.SlotId || (!string.IsNullOrWhiteSpace(timeSlot) && a.TimeSlot == timeSlot.Trim())) &&
                a.Status != AppointmentStatus.CANCELLED);

            if (slotBooked >= slot.Capacity)
            {
                throw new HttpException(StatusCodes.Status400BadRequest,
                    $"Khung giờ {timeSlot ?? $"{slot.StartTime:HH\\:mm}-{slot.EndTime:HH\\:mm}"} ngày {appointmentDate:dd/MM/yyyy} đã đủ số lượng người đăng ký (sức chứa tối đa: {slot.Capacity} người). Vui lòng chọn khung giờ khác.");
            }
        }
    }

    public async Task ValidateNoDuplicateBookingAsync(int donorId, DateOnly appointmentDate)
    {
        var hasActive = await _context.DonationAppointments
            .AnyAsync(a => a.DonorId == donorId &&
                           a.AppointmentDate == appointmentDate &&
                           a.Status == AppointmentStatus.BOOKED);
        if (hasActive)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Người hiến đã có lịch hẹn đang chờ tiếp nhận (BOOKED) vào ngày {appointmentDate:dd/MM/yyyy}.");
        }
    }
}