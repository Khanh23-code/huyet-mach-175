using HuyetMach175.Modules.Inventory.DTOs.Inventory;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IInventoryService
{
    Task<InventorySummaryResponse> GetInventorySummaryAsync(InventorySummaryFilterDto filter);
}