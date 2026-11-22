using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Modules.Donation.Services;

public interface IAppointmentValidator
{
    Task<Donor> ValidateDonorAsync(int donorId);
    void ValidateEligibility(Donor donor, DateOnly targetDate);
    Task<DonationCampaignSlot?> ValidateSlotAsync(int? slotId, DateOnly appointmentDate);
    Task ValidateNoDuplicateBookingAsync(int donorId, DateOnly appointmentDate);
}
