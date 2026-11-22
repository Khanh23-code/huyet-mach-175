using HuyetMach175.Modules.Donation.DTOs.Appointments;

namespace HuyetMach175.Modules.Donation.Services;

public interface ICheckInService
{
    Task<AppointmentDetailDto> CheckInAsync(CheckInAppointmentRequest request);
    Task<AppointmentDetailDto> CreateWalkInAsync(CreateWalkInAppointmentRequest request);
}

