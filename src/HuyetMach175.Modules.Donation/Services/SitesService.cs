using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Donation.DTOs.Sites;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class SitesService : ISitesService
{
    private readonly ApplicationDbContext _context;

    public SitesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SiteDto>> GetSitesAsync(bool? isActive, string? keyword)
    {
        IQueryable<DonationSite> query = _context.DonationSites
            .AsNoTracking()
            .Include(s => s.Slots);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(s => EF.Functions.ILike(s.SiteName, $"%{kw}%") || EF.Functions.ILike(s.Address, $"%{kw}%"));
        }

        var sites = await query.OrderBy(s => s.SiteType).ThenBy(s => s.SiteId).ToListAsync();
        return sites.Select(SiteMapper.ToDto).ToList();
    }

    public async Task<SiteDto> GetSiteByIdAsync(int id)
    {
        if (id <= 0)
            throw new HttpException(StatusCodes.Status400BadRequest, "ID điểm hiến không hợp lệ.");

        var site = await _context.DonationSites
            .AsNoTracking()
            .Include(s => s.Slots)
            .FirstOrDefaultAsync(s => s.SiteId == id);

        if (site == null)
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy điểm hiến với mã #{id}.");

        return SiteMapper.ToDto(site);
    }
}
