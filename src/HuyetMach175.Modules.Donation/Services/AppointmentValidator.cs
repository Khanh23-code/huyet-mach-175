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

    public async Task ValidateCampaignAsync(int? campaignId, DateOnly appointmentDate)
    {
        if (!campaignId.HasValue) return;

        var campaign = await _context.DonationCampaigns.FirstOrDefaultAsync(c => c.CampaignId == campaignId.Value);
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

