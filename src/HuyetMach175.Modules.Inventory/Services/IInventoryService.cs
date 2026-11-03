using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.Inventory;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IInventoryService
{
    Task<InventorySummaryResponse> GetInventorySummaryAsync(InventorySummaryFilterDto filter);
    Task<PagedResult<ExpiringSoonBagDto>> GetExpiringSoonBagsAsync(ExpiringSoonFilterDto filter);
    Task<RelocateBloodBagResponse> RelocateBloodBagAsync(int bagId, RelocateBloodBagRequest request, int userId, string? ipAddress);
}