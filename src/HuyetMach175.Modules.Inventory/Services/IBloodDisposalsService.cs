using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.DTOs.Common;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodDisposalsService
{
    Task<PagedResult<DisposalRecordDto>> GetDisposalsAsync(DisposalFilterDto filter);
    Task<DisposalDetailDto> GetDisposalByIdAsync(int id);
}