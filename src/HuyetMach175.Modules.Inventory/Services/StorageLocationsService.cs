using HuyetMach175.Api.Data;
using HuyetMach175.Modules.Inventory.DTOs.StorageLocations;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class StorageLocationsService : IStorageLocationsService
{
    private readonly ApplicationDbContext _context;

    public StorageLocationsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StorageLocationDto>> GetStorageLocationsAsync(bool? availableOnly = null)
    {
        var query = _context.StorageLocations.AsNoTracking();

        if (!availableOnly.HasValue || availableOnly.Value)
        {
            query = query.Where(l => l.IsActive && !l.IsFull);
        }

        return await query
            .OrderBy(l => l.RefrigeratorName)
            .ThenBy(l => l.ShelfNumber)
            .Select(l => new StorageLocationDto
            {
                LocationId = l.LocationId,
                RefrigeratorName = l.RefrigeratorName,
                ShelfNumber = l.ShelfNumber,
                TargetTemperature = l.TargetTemperature,
                IsFull = l.IsFull,
                IsActive = l.IsActive
            })
            .ToListAsync();
    }
}