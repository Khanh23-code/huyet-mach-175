using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Proposals;

namespace HuyetMach175.Modules.Donation.Services;

public interface IProposalsService
{
    Task<ProposalDto> CreateAsync(CreateProposalRequest request, int userId);
    Task<PagedResult<ProposalDto>> GetMyProposalsAsync(ProposalFilterDto filter, int userId);
    Task<PagedResult<ProposalDto>> GetProposalsAsync(ProposalFilterDto filter);
    Task<ProposalDto> GetByIdAsync(int id);
    Task<ProposalDto> ApproveAsync(int id, int userId);
    Task<ProposalDto> RejectAsync(int id, RejectProposalRequest request, int userId);
}
