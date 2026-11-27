using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Surveys;

namespace HuyetMach175.Modules.Donation.Services;

public interface ISurveysService
{
    Task<PagedResult<SurveyQueueItemDto>> GetScreeningQueueAsync(SurveyQueueFilterDto filter);
    Task<SurveyDetailDto> GetSurveyByAppointmentIdAsync(Guid appointmentId);
    Task<SubmitOnlineSurveyResponse> SubmitOnlineSurveyAsync(SubmitOnlineSurveyRequest request);
    Task<ClinicalScreeningResponse> UpdateClinicalScreeningAsync(Guid appointmentId, UpdateClinicalScreeningRequest request, Guid doctorId);
}