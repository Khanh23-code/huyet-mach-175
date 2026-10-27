using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Sessions;

namespace HuyetMach175.Modules.Donation.Services;

public interface IDonationSessionsService
{
    Task<PagedResult<SessionQueueItemDto>> GetSessionsAsync(SessionQueueFilterDto filter);
    Task<SessionDetailDto> GetSessionByIdAsync(int id);
}