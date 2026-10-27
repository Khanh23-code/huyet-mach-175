using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Surveys;

namespace HuyetMach175.Modules.Donation.Services;

public interface ISurveysService
{
    Task<PagedResult<SurveyQueueItemDto>> GetScreeningQueueAsync(SurveyQueueFilterDto filter);
    Task<SurveyDetailDto> GetSurveyByAppointmentIdAsync(int appointmentId);
    Task<SubmitOnlineSurveyResponse> SubmitOnlineSurveyAsync(SubmitOnlineSurveyRequest request);
    Task<ClinicalScreeningResponse> UpdateClinicalScreeningAsync(int appointmentId, UpdateClinicalScreeningRequest request, int doctorId);
}