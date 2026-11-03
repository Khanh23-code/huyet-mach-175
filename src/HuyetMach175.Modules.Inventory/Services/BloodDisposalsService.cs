using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class BloodDisposalsService : IBloodDisposalsService
{
    private readonly ApplicationDbContext _context;

    public BloodDisposalsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DisposalRecordDto>> GetDisposalsAsync(DisposalFilterDto filter)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.BloodDisposalRecords
            .AsNoTracking()
            .Include(r => r.BloodBag)
                .ThenInclude(b => b!.ComponentType)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<DisposalStatus>(filter.Status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Reason))
        {
            if (Enum.TryParse<DisposalReason>(filter.Reason.Trim(), true, out var parsedReason))
            {
                query = query.Where(r => r.Reason == parsedReason);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(r =>
                EF.Functions.ILike(r.DisposalCode, $"%{kw}%") ||
                (r.BloodBag != null && EF.Functions.ILike(r.BloodBag.Barcode, $"%{kw}%")));
        }

        if (filter.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(r => r.RequestedAt >= fromUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(r => r.RequestedAt <= toUtc);
        }

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderByDescending(r => r.RequestedAt)
            .ThenByDescending(r => r.DisposalId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = records.Select(r => r.ToRecordDto()).ToList();

        return new PagedResult<DisposalRecordDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<DisposalDetailDto> GetDisposalByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu đề xuất tiêu hủy không hợp lệ.");
        }

        var r = await _context.BloodDisposalRecords
            .AsNoTracking()
            .Include(r => r.BloodBag)
                .ThenInclude(b => b!.ComponentType)
            .Include(r => r.BloodBag)
                .ThenInclude(b => b!.Location)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .FirstOrDefaultAsync(r => r.DisposalId == id);

        if (r == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu đề xuất tiêu hủy với mã #{id}.");
        }

        return r.ToDetailDto();
    }
}
