using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.LabTests;

namespace HuyetMach175.Modules.Inventory.Services;

public interface ILabTestsService
{
    Task<PagedResult<LabTestQueueItemDto>> GetLabTestQueueAsync(LabTestQueueFilterDto filter);
    Task<LabTestResultDto> RecordLabTestAsync(RecordLabTestRequest request, int userId, string? ipAddress);
    Task<LabTestResultDto> GetLabTestByBagIdentifierAsync(string identifier);
}
