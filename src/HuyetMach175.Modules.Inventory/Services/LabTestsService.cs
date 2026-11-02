using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.LabTests;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class LabTestsService : ILabTestsService
{
    private readonly ApplicationDbContext _context;

    public LabTestsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LabTestQueueItemDto>> GetLabTestQueueAsync(LabTestQueueFilterDto filter)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.BloodBags
            .AsNoTracking()
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .Include(b => b.Session)
                .ThenInclude(s => s!.Appointment)
                    .ThenInclude(a => a!.Donor)
            .Where(b => b.Status == BagStatus.READY_TESTING && !b.TestResults.Any())
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.Barcode, $"%{kw}%") ||
                (b.Session != null && b.Session.Appointment != null && b.Session.Appointment.Donor != null &&
                    (EF.Functions.ILike(b.Session.Appointment.Donor.FullName, $"%{kw}%") ||
                     EF.Functions.ILike(b.Session.Appointment.Donor.CitizenId, $"%{kw}%"))));
        }

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(b => b.CollectedAt)
            .ThenBy(b => b.BagId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = entities.Select(b =>
        {
            var donor = b.Session?.Appointment?.Donor;
            var bloodGroup = $"{(b.BloodType == BloodType.AB ? "AB" : b.BloodType.ToString())}{(b.RhFactor == RhFactor.POSITIVE ? "+" : "-")}";

            return new LabTestQueueItemDto
            {
                BagId = b.BagId,
                Barcode = b.Barcode,
                DonorCode = donor?.CitizenId,
                DonorName = donor != null ? donor.FullName : "Tiếp nhận ngoài viện",
                ComponentTypeCode = b.ComponentType?.TypeCode.ToString() ?? string.Empty,
                ComponentTypeName = b.ComponentType?.TypeName ?? string.Empty,
                VolumeMl = b.VolumeMl,
                ExpectedBloodType = b.BloodType.ToString(),
                ExpectedRhFactor = b.RhFactor.ToString(),
                ExpectedBloodGroup = bloodGroup,
                StorageLocationId = b.LocationId,
                StorageLocationName = b.Location != null ? $"{b.Location.RefrigeratorName} - {b.Location.ShelfNumber}" : null,
                CollectedAt = b.CollectedAt,
                Status = b.Status.ToString()
            };
        }).ToList();

        return new PagedResult<LabTestQueueItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }
}