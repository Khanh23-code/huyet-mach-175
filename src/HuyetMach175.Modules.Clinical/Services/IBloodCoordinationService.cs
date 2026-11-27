using HuyetMach175.Modules.Clinical.DTOs.BloodCoordination;
using HuyetMach175.Modules.Clinical.DTOs.Common;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodCoordinationService
{
    Task<SupplyDemandResponse> GetSupplyDemandAsync(Guid componentTypeId);
    Task<PagedResult<CoordinationQueueItemDto>> GetCoordinationQueueAsync(BloodCoordinationQueueFilterDto filter);
    Task<List<AvailableBloodBagDto>> GetAvailableBagsForItemAsync(Guid requestId, Guid requestItemId, bool includeCompatible);
    Task<AllocateBloodBagsResponse> AllocateBagsAsync(Guid requestId, Guid requestItemId, AllocateBloodBagsRequest request, Guid userId, string? ipAddress);
}
