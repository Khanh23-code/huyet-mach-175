using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Modules.Donation.Services;

public interface IAppointmentValidator
{
    Task<Donor> ValidateDonorAsync(int donorId);
    void ValidateEligibility(Donor donor, DateOnly targetDate);
    Task ValidateCampaignAsync(int? campaignId, DateOnly appointmentDate, string? timeSlot = null, int? slotId = null);
    Task ValidateNoDuplicateBookingAsync(int donorId, DateOnly appointmentDate);
}
