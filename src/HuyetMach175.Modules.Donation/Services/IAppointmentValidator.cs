using HuyetMach175.Api.Data.Entities;

namespace HuyetMach175.Modules.Donation.Services;

public interface IAppointmentValidator
{
    Task<Donor> ValidateDonorAsync(Guid donorId);
    void ValidateEligibility(Donor donor, DateOnly targetDate);
    Task<DonationCampaignSlot?> ValidateSlotAsync(Guid? slotId, DateOnly appointmentDate);
    Task ValidateNoDuplicateBookingAsync(Guid donorId, DateOnly appointmentDate);
}
