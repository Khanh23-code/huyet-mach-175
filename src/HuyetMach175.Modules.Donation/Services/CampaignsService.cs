using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Donation.DTOs.Campaigns;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class CampaignsService : ICampaignsService
{
    private readonly ApplicationDbContext _context;

    public CampaignsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CampaignListItemDto>> GetCampaignsAsync(CampaignFilterDto filter)
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

        var query = _context.DonationCampaigns
            .AsNoTracking();

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
            .OrderBy(c => c.CampaignId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new CampaignListItemDto
            {
                CampaignId = c.CampaignId,
                CampaignName = c.CampaignName,
                LocationName = c.LocationName,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                TargetDonations = c.TargetDonations,
                Status = c.Status,
                TotalRegistered = c.Appointments.Count(a => a.Status != AppointmentStatus.CANCELLED),
                TotalCollected = c.Appointments.Count(a => a.DonationSession != null && a.DonationSession.Status == SessionStatus.COMPLETED),
                CreatedBy = c.CreatedBy,
                CreatedByName = c.CreatedByUser != null ? c.CreatedByUser.FullName : string.Empty,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<CampaignListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
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
            .Include(c => c.Appointments)
                .ThenInclude(a => a.DonationSession)
            .FirstOrDefaultAsync(c => c.CampaignId == id);

        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        var totalRegistered = campaign.Appointments.Count(a => a.Status != AppointmentStatus.CANCELLED);
        var totalCheckedIn = campaign.Appointments.Count(a => a.CheckedInAt != null || a.Status == AppointmentStatus.CHECKED_IN);
        var totalCollected = campaign.Appointments.Count(a => a.DonationSession != null && a.DonationSession.Status == SessionStatus.COMPLETED);
        var completionRate = campaign.TargetDonations > 0
            ? Math.Round((double)totalCollected / campaign.TargetDonations * 100, 2)
            : 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var remainingDays = campaign.EndDate >= today ? campaign.EndDate.DayNumber - today.DayNumber : 0;

        return new CampaignDetailDto
        {
            CampaignId = campaign.CampaignId,
            CampaignName = campaign.CampaignName,
            LocationName = campaign.LocationName,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            TargetDonations = campaign.TargetDonations,
            Status = campaign.Status,
            CreatedBy = campaign.CreatedBy,
            CreatedByName = campaign.CreatedByUser != null ? campaign.CreatedByUser.FullName : string.Empty,
            ApprovedBy = campaign.ApprovedBy,
            ApprovedByName = campaign.ApprovedByUser != null ? campaign.ApprovedByUser.FullName : null,
            ApprovedAt = campaign.ApprovedAt,
            CancelledBy = campaign.CancelledBy,
            CancelledByName = campaign.CancelledByUser != null ? campaign.CancelledByUser.FullName : null,
            CancelledAt = campaign.CancelledAt,
            RejectionReason = campaign.RejectionReason,
            CreatedAt = campaign.CreatedAt,
            TotalRegistered = totalRegistered,
            TotalCheckedIn = totalCheckedIn,
            TotalCollected = totalCollected,
            CompletionRate = completionRate,
            RemainingDays = remainingDays
        };
    }

    public async Task<CampaignDetailDto> CreateCampaignAsync(CreateCampaignRequest request, int userId)
    {
        if (request.EndDate < request.StartDate)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.StartDate < today)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Ngày bắt đầu đợt hiến máu không thể trong quá khứ.");
        }

        if (request.TargetDonations <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Chỉ tiêu thu gom phải lớn hơn 0 túi máu.");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Người dùng không hợp lệ hoặc đã bị vô hiệu hóa.");
        }

        var campaign = new DonationCampaign
        {
            CampaignName = request.CampaignName.Trim(),
            LocationName = request.LocationName.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TargetDonations = request.TargetDonations,
            Status = CampaignStatus.DRAFT,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

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

        var campaign = await _context.DonationCampaigns.FindAsync(id);
        if (campaign == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy đợt hiến máu với mã #{id}.");
        }

        if (campaign.Status != CampaignStatus.DRAFT && campaign.Status != CampaignStatus.REJECTED)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Không thể chỉnh sửa đợt hiến máu ở trạng thái '{campaign.Status}'. Chỉ cho phép chỉnh sửa khi ở trạng thái Bản nháp (DRAFT) hoặc Bị từ chối (REJECTED).");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        if (request.TargetDonations <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Chỉ tiêu thu gom phải lớn hơn 0 túi máu.");
        }

        campaign.CampaignName = request.CampaignName.Trim();
        campaign.LocationName = request.LocationName.Trim();
        campaign.StartDate = request.StartDate;
        campaign.EndDate = request.EndDate;
        campaign.TargetDonations = request.TargetDonations;

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

        if (campaign.Status != CampaignStatus.DRAFT && campaign.Status != CampaignStatus.REJECTED)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Không thể gửi phê duyệt đợt hiến máu ở trạng thái '{campaign.Status}'. Chỉ áp dụng cho trạng thái Bản nháp (DRAFT) hoặc Bị từ chối (REJECTED).");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (campaign.EndDate < today)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Đợt hiến máu đã quá hạn (ngày kết thúc trong quá khứ). Vui lòng cập nhật lại thời gian trước khi gửi duyệt.");
        }

        campaign.Status = CampaignStatus.PENDING_APPROVAL;
        campaign.RejectionReason = null;
        campaign.ApprovedBy = null;
        campaign.ApprovedAt = null;

        await _context.SaveChangesAsync();

        return await GetCampaignByIdAsync(campaign.CampaignId);
    }
}