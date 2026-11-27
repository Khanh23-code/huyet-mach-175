using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.DTOs.Common;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodDisposalsService
{
    Task<PagedResult<DisposalRecordDto>> GetDisposalsAsync(DisposalFilterDto filter);
    Task<DisposalDetailDto> GetDisposalByIdAsync(Guid id);
    Task<CreateDisposalResponse> CreateDisposalAsync(CreateDisposalRequest request, Guid userId, string? ipAddress);
    Task<DisposalDetailDto> CancelDisposalAsync(Guid id, CancelDisposalRequest? request, Guid userId, string? ipAddress);
    Task<DisposalDetailDto> ApproveDisposalAsync(Guid id, ApproveDisposalRequest request, Guid userId, string? ipAddress);
    Task<PagedResult<DisposalCandidateDto>> GetDisposalCandidatesAsync(DisposalCandidateFilterDto filter);
    Task<DisposalDetailDto> ExecuteDisposalAsync(Guid id, Guid userId, string? ipAddress);
}
