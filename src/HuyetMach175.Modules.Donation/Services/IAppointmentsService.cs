using HuyetMach175.Modules.Donation.DTOs.Appointments;
using HuyetMach175.Modules.Donation.DTOs.Common;

namespace HuyetMach175.Modules.Donation.Services;

public interface IAppointmentsService
{
    Task<PagedResult<AppointmentListItemDto>> GetAppointmentsAsync(AppointmentFilterDto filter);
    Task<AppointmentDetailDto> GetAppointmentByIdAsync(int id);
    Task<AppointmentDetailDto> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<AppointmentDetailDto> CancelAppointmentAsync(int id, CancelAppointmentRequest? request);
}
