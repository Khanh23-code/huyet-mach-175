using HuyetMach175.Api.Data;
using HuyetMach175.Modules.Donation.DTOs.Common;
using HuyetMach175.Modules.Donation.DTOs.Donors;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Donation.Services;

public class DonorsService : IDonorsService
{
    private readonly ApplicationDbContext _context;

    public DonorsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DonorListItemDto>> GetDonorsAsync(DonorFilterDto filter)
    {
        if (filter.Page < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.Donors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(d =>
                EF.Functions.ILike(d.CitizenId, $"%{kw}%") ||
                EF.Functions.ILike(d.FullName, $"%{kw}%") ||
                EF.Functions.ILike(d.PhoneNumber, $"%{kw}%"));
        }

        if (filter.BloodType.HasValue)
        {
            query = query.Where(d => d.BloodType == filter.BloodType.Value);
        }

        if (filter.RhFactor.HasValue)
        {
            query = query.Where(d => d.RhFactor == filter.RhFactor.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.DonorId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DonorListItemDto
            {
                DonorId = d.DonorId,
                CitizenId = d.CitizenId,
                FullName = d.FullName,
                DateOfBirth = d.DateOfBirth,
                Gender = d.Gender,
                PhoneNumber = d.PhoneNumber,
                Email = d.Email,
                BloodType = d.BloodType,
                RhFactor = d.RhFactor,
                TotalDonations = d.TotalDonations,
                LastDonationDate = d.LastDonationDate
            })
            .ToListAsync();

        return new PagedResult<DonorListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }
}