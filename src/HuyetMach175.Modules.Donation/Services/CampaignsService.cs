using HuyetMach175.Api.Data;
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
            .OrderByDescending(c => c.CreatedAt)
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
}