using HuyetMach175.Modules.Inventory.DTOs.BloodBags;
using HuyetMach175.Modules.Inventory.DTOs.Common;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodBagsService
{
    Task<ReceiveBloodBagResponse> ReceiveBloodBagAsync(ReceiveBloodBagRequest request, int userId, string? ipAddress);
    Task<PagedResult<BloodBagDto>> GetBloodBagsAsync(BloodBagFilterDto filter);
    Task<RegisterExternalBloodBagResponse> RegisterExternalBloodBagAsync(RegisterExternalBloodBagRequest request, int userId, string? ipAddress);
}
