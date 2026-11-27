using HuyetMach175.Modules.Inventory.DTOs.BloodBags;
using HuyetMach175.Modules.Inventory.DTOs.Common;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodBagsService
{
    Task<ReceiveBloodBagResponse> ReceiveBloodBagAsync(ReceiveBloodBagRequest request, Guid userId, string? ipAddress);
    Task<PagedResult<BloodBagDto>> GetBloodBagsAsync(BloodBagFilterDto filter);
    Task<BloodBagScanResponse> ScanBloodBagAsync(string barcode);
    Task<TransitionBagStatusResponse> TransitionStatusAsync(TransitionBagStatusRequest request, Guid userId, string? ipAddress);
    Task<RecentStatusLogsResponse> GetRecentStatusLogsAsync(int limit);
    Task<RegisterExternalBloodBagResponse> RegisterExternalBloodBagAsync(RegisterExternalBloodBagRequest request, Guid userId, string? ipAddress);
}
