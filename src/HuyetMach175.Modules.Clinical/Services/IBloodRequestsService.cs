using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.Modules.Clinical.DTOs.Common;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodRequestsService
{
    Task<CreateBloodRequestResponse> CreateBloodRequestAsync(CreateBloodRequestDto request, int doctorId, string? ipAddress);
    Task<PagedResult<ClinicalBloodRequestDto>> GetClinicalBloodRequestsAsync(ClinicalBloodRequestFilterDto filter, int doctorId);
    Task<BloodRequestDetailDto> GetBloodRequestByIdAsync(int id, int currentUserId);
    Task<RejectBloodRequestResponse> RejectBloodRequestAsync(int id, RejectBloodRequestDto request, int userId, string? ipAddress);
}
