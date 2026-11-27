using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.Modules.Clinical.DTOs.Common;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodRequestsService
{
    Task<CreateBloodRequestResponse> CreateBloodRequestAsync(CreateBloodRequestDto request, Guid doctorId, string? ipAddress);
    Task<BloodRequestDetailDto> UpdateDraftBloodRequestAsync(Guid id, UpdateBloodRequestDto request, Guid doctorId, string? ipAddress);
    Task<SubmitBloodRequestResponse> SubmitDraftBloodRequestAsync(Guid id, Guid doctorId, string? ipAddress);
    Task<PagedResult<ClinicalBloodRequestDto>> GetClinicalBloodRequestsAsync(ClinicalBloodRequestFilterDto filter, Guid doctorId);
    Task<BloodRequestDetailDto> GetBloodRequestByIdAsync(Guid id, Guid currentUserId);
    Task<RejectBloodRequestResponse> RejectBloodRequestAsync(Guid id, RejectBloodRequestDto request, Guid userId, string? ipAddress);
}
