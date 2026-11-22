using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.Processing;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodProcessingService
{
    Task<PagedResult<ProcessingQueueItemDto>> GetProcessingQueueAsync(ProcessingQueueFilterDto filter);
    Task<SeparateBloodBagResponse> SeparateBloodBagAsync(SeparateBloodBagRequest request, int userId, string? ipAddress);
    Task<List<ComponentTypeOptionDto>> GetSeparableComponentTypesAsync();
}