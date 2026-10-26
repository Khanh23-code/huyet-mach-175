using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class CampaignsService : ICampaignsService
{
    private readonly ApplicationDbContext _context;
    private readonly ICampaignValidator _validator;

    public CampaignsService(ApplicationDbContext context, ICampaignValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<PagedResult<CampaignListItemDto>> GetCampaignsAsync(CampaignFilterDto filter, int? createdBy = null)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (PageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (PageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value > filter.ToDate.Value)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                "Khoảng thời gian không hợp lệ: 'Từ ngày' (FromDate) phải nhỏ hơn hoặc bằng 'Đến ngày' (ToDate).");
        }

        var query = _context.DonationCampaigns.AsNoTracking();

        if (createdBy.HasValue)
        {
            query = query.Where(c => c.CreatedBy == createdBy.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(c => c.EndDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(c => c.StartDate <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(c => EF.Functions.ILike(c.CampaignName, $"%{kw}%") || EF.Functions.ILike(c.LocationName, $"%{kw}%"));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(CampaignMappings.ProjectToListItem)
            .ToListAsync();

        return new PagedResult<CampaignListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public Task<PagedResult<CampaignListItemDto>> GetMyCampaignsAsync(CampaignFilterDto filter, int userId)
    {
        if (userId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người dùng không hợp lệ.");
        }

        return GetCampaignsAsync(filter, createdBy: userId);
    }

    public async Task<CampaignDetailDto> GetCampaignByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .AsNoTracking()
            .Include(c => c.CreatedByUser)
            .Include(c => c.ApprovedByUser)
            .Include(c => c.CancelledByUser)
            .Include(c => c.Slots)
            .Include(c => c.Appointments)
                .ThenInclude(a => a.DonationSession)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        return campaign.ToDetailDto();
    }

    public async Task<CampaignDetailDto> CreateCampaignAsync(CreateCampaignRequest request, int userId)
    {
        _validator.ValidateDates(request.StartDate, request.EndDate, isNew: true);
        _validator.ValidateTargetDonations(request.TargetDonations);

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người dùng không hợp lệ hoặc đã bị vô hiệu hóa.");
        }

        var campaign = new DonationCampaign
        {
            CampaignName = request.CampaignName.Trim(),
            Description = request.Description?.Trim(),
            LocationName = request.LocationName.Trim(),
            CampaignType = request.CampaignType,
            OperatingDays = request.CampaignType == CampaignType.RECURRING ? request.OperatingDays?.Trim() : null,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TargetDonations = request.TargetDonations,
            MaxCapacity = request.MaxCapacity,
            Status = CampaignStatus.DRAFT,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Slots != null && request.Slots.Any())
        {
            foreach (var slot in request.Slots)
            {
                campaign.Slots.Add(new DonationCampaignSlot
                {
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Capacity = slot.Capacity > 0 ? slot.Capacity : 20
                });
            }
        }

        _context.DonationCampaigns.Add(campaign);
        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> UpdateCampaignAsync(int id, UpdateCampaignRequest request)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .Include(c => c.Slots)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateEditable(campaign);
        _validator.ValidateDates(request.StartDate, request.EndDate, isNew: false);
        _validator.ValidateTargetDonations(request.TargetDonations);

        campaign.CampaignName = request.CampaignName.Trim();
        campaign.Description = request.Description?.Trim();
        campaign.LocationName = request.LocationName.Trim();
        campaign.CampaignType = request.CampaignType;
        campaign.OperatingDays = request.CampaignType == CampaignType.RECURRING ? request.OperatingDays?.Trim() : null;
        campaign.StartDate = request.StartDate;
        campaign.EndDate = request.EndDate;
        campaign.TargetDonations = request.TargetDonations;
        campaign.MaxCapacity = request.MaxCapacity;

        if (request.Slots != null)
        {
            _context.DonationCampaignSlots.RemoveRange(campaign.Slots);
            foreach (var slot in request.Slots)
            {
                campaign.Slots.Add(new DonationCampaignSlot
                {
                    CampaignId = campaign.CampaignId,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Capacity = slot.Capacity > 0 ? slot.Capacity : 20
                });
            }
        }

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> SubmitForApprovalAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateSubmittable(campaign);

        campaign.Status = CampaignStatus.PENDING_APPROVAL;
        campaign.RejectionReason = null;

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> ApproveCampaignAsync(int id, ApproveCampaignRequest request, int userId)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateApprovable(campaign, request.IsApproved, request.RejectionReason);

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người duyệt không hợp lệ hoặc đã bị vô hiệu hóa.");
        }

        if (request.IsApproved)
        {
            campaign.Status = CampaignStatus.APPROVED;
            campaign.ApprovedBy = userId;
            campaign.ApprovedAt = DateTime.UtcNow;
            campaign.RejectionReason = null;
        }
        else
        {
            campaign.Status = CampaignStatus.REJECTED;
            campaign.ApprovedBy = userId;
            campaign.ApprovedAt = DateTime.UtcNow;
            campaign.RejectionReason = request.RejectionReason!.Trim();
        }

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task<CampaignDetailDto> UpdateCampaignStatusAsync(int id, UpdateCampaignStatusRequest request, int userId)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateStatusTransition(campaign, request.Status);

        campaign.Status = request.Status;
        if (request.Status == CampaignStatus.CANCELLED)
        {
            campaign.CancelledBy = userId;
            campaign.CancelledAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }

    public async Task DeleteCampaignAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "ID đợt hiến máu không hợp lệ.");
        }

        var campaign = await _context.DonationCampaigns
            .Include(c => c.Appointments)
            .Include(c => c.Slots)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        _validator.ValidateDeletable(campaign);

        _context.DonationCampaignSlots.RemoveRange(campaign.Slots);
        _context.DonationCampaigns.Remove(campaign);
        await _context.SaveChangesAsync();
    }
}