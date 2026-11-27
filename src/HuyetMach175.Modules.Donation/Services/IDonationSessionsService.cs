using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Sessions;

namespace HuyetMach175.Modules.Donation.Services;

public interface IDonationSessionsService
{
    Task<PagedResult<SessionQueueItemDto>> GetSessionsAsync(SessionQueueFilterDto filter);
    Task<SessionDetailDto> GetSessionByIdAsync(Guid id);
    Task<CollectionResponse> CompleteCollectionAsync(Guid sessionId, UpdateCollectionRequest request, Guid phlebotomistId);
    Task<StartCollectionResponse> StartCollectionAsync(Guid sessionId, Guid phlebotomistId);
}