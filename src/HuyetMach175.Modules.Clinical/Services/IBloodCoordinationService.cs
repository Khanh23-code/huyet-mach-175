using HuyetMach175.Modules.Clinical.DTOs.BloodCoordination;
using HuyetMach175.Modules.Clinical.DTOs.Common;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodCoordinationService
{
    Task<SupplyDemandResponse> GetSupplyDemandAsync(int componentTypeId);
    Task<PagedResult<CoordinationQueueItemDto>> GetCoordinationQueueAsync(BloodCoordinationQueueFilterDto filter);
    Task<List<AvailableBloodBagDto>> GetAvailableBagsForItemAsync(int requestId, int requestItemId, bool includeCompatible);
    Task<AllocateBloodBagsResponse> AllocateBagsAsync(int requestId, int requestItemId, AllocateBloodBagsRequest request, int userId, string? ipAddress);
}
