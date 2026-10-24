using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Proposals;
using HuyetMach175.Modules.Donation.Services;
using HuyetMach175.SharedKernel.Authorization;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.Controllers;

[ApiController]
[Route("api/proposals")]
[Authorize]
[Tags("Donation.Proposals")]
public class ProposalsController : ControllerBase
{
    private readonly IProposalsService _proposalsService;

    public ProposalsController(IProposalsService proposalsService)
    {
        _proposalsService = proposalsService;
    }

    [HttpPost]
    [HasPermission("CAMPAIGN_EDIT")]
    [EndpointSummary("Gửi đề xuất tạo điểm hiến mới / đổi lịch & sức chứa")]
    [EndpointDescription(
        "BBNK gửi đề xuất, trạng thái ban đầu **PENDING**, chờ Quản lý (MGT) duyệt.\n\n" +
        "- `proposalType = NEW_SITE`: bắt buộc siteName, siteType (FIXED|MOBILE), address, phoneNumber (10 số, bắt đầu bằng 0), slots.\n" +
        "- `proposalType = UPDATE_SCHEDULE`: bắt buộc siteId, slots.\n\n" +
        "**Quyền:** CAMPAIGN_EDIT.")]
    [ProducesResponseType(typeof(ProposalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProposalRequest request)
    {
        var result = await _proposalsService.CreateAsync(request, User.GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.ProposalId }, result);
    }

    [HttpGet("mine")]
    [HasPermission("CAMPAIGN_EDIT")]
    [EndpointSummary("Đề xuất của tôi")]
    [ProducesResponseType(typeof(PagedResult<ProposalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] ProposalFilterDto filter)
    {
        return Ok(await _proposalsService.GetMyProposalsAsync(filter, User.GetUserId()));
    }

    [HttpGet]
    [HasPermission("CAMPAIGN_APPROVE")]
    [EndpointSummary("Danh sách đề xuất (dành cho người duyệt)")]
    [ProducesResponseType(typeof(PagedResult<ProposalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ProposalFilterDto filter)
    {
        return Ok(await _proposalsService.GetProposalsAsync(filter));
    }

    [HttpGet("{id:int}")]
    [HasPermission("CAMPAIGN_APPROVE")]
    [EndpointSummary("Chi tiết đề xuất kèm cấu hình hiện tại")]
    [ProducesResponseType(typeof(ProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _proposalsService.GetByIdAsync(id));
    }

    [HttpPatch("{id:int}/approve")]
    [HasPermission("CAMPAIGN_APPROVE")]
    [EndpointSummary("Phê duyệt và kích hoạt đề xuất")]
    [EndpointDescription("Tạo điểm hiến mới hoặc áp dụng lịch/sức chứa mới ngay lập tức. **Quyền:** CAMPAIGN_APPROVE (MGT).")]
    [ProducesResponseType(typeof(ProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(int id)
    {
        return Ok(await _proposalsService.ApproveAsync(id, User.GetUserId()));
    }

    [HttpPatch("{id:int}/reject")]
    [HasPermission("CAMPAIGN_CANCEL")]
    [EndpointSummary("Từ chối đề xuất (bắt buộc lý do)")]
    [EndpointDescription("**Quyền:** CAMPAIGN_CANCEL (MGT).")]
    [ProducesResponseType(typeof(ProposalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectProposalRequest request)
    {
        return Ok(await _proposalsService.RejectAsync(id, request, User.GetUserId()));
    }
}
