using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.Modules.Clinical.DTOs.Common;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodRequestsService
{
    Task<CreateBloodRequestResponse> CreateBloodRequestAsync(CreateBloodRequestDto request, int doctorId, string? ipAddress);
    Task<PagedResult<ClinicalBloodRequestDto>> GetClinicalBloodRequestsAsync(ClinicalBloodRequestFilterDto filter, int doctorId);
    Task<BloodRequestDetailDto> GetBloodRequestByIdAsync(int id, int currentUserId);
    Task<SupplyDemandResponse> GetSupplyDemandAsync(int componentTypeId);
    Task<PagedResult<CoordinationQueueItemDto>> GetCoordinationQueueAsync(BloodCoordinationQueueFilterDto filter);
    Task<List<AvailableBloodBagDto>> GetAvailableBagsForItemAsync(int requestId, int requestItemId, bool includeCompatible);
    Task<AllocateBloodBagsResponse> AllocateBagsAsync(int requestId, int requestItemId, AllocateBloodBagsRequest request, int userId, string? ipAddress);
    Task<RejectBloodRequestResponse> RejectBloodRequestAsync(int id, RejectBloodRequestDto request, int userId, string? ipAddress);
}
