using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.StorageLocations;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class StorageLocationsService : IStorageLocationsService
{
    private readonly ApplicationDbContext _context;

    public StorageLocationsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(StorageLocationFilterDto filter)
    {
        var query = _context.StorageLocations.AsNoTracking();

        if (filter.AvailableOnly == true)
        {
            query = query.Where(l => l.IsActive && !l.IsFull);
        }
        else
        {
            if (filter.IsActive.HasValue)
            {
                query = query.Where(l => l.IsActive == filter.IsActive.Value);
            }

            if (filter.IsFull.HasValue)
            {
                query = query.Where(l => l.IsFull == filter.IsFull.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(l =>
                l.RefrigeratorName.ToLower().Contains(search) ||
                l.ShelfNumber.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 ? Math.Min(filter.PageSize, 100) : 20;

        var items = await query
            .OrderBy(l => l.RefrigeratorName)
            .ThenBy(l => l.ShelfNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new StorageLocationDto
            {
                LocationId = l.LocationId,
                RefrigeratorName = l.RefrigeratorName,
                ShelfNumber = l.ShelfNumber,
                TargetTemperature = l.TargetTemperature,
                IsFull = l.IsFull,
                IsActive = l.IsActive,
                CurrentBagsCount = l.BloodBags.Count(b =>
                    b.Status == BagStatus.AVAILABLE ||
                    b.Status == BagStatus.READY_TESTING ||
                    b.Status == BagStatus.TESTED_PASSED ||
                    b.Status == BagStatus.LOCKED ||
                    b.Status == BagStatus.RESERVED)
            })
            .ToListAsync();

        return new PagedResult<StorageLocationDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<StorageLocationDto> GetStorageLocationByIdAsync(int id)
    {
        var location = await _context.StorageLocations
            .AsNoTracking()
            .Where(l => l.LocationId == id)
            .Select(l => new StorageLocationDto
            {
                LocationId = l.LocationId,
                RefrigeratorName = l.RefrigeratorName,
                ShelfNumber = l.ShelfNumber,
                TargetTemperature = l.TargetTemperature,
                IsFull = l.IsFull,
                IsActive = l.IsActive,
                CurrentBagsCount = l.BloodBags.Count(b =>
                    b.Status == BagStatus.AVAILABLE ||
                    b.Status == BagStatus.READY_TESTING ||
                    b.Status == BagStatus.TESTED_PASSED ||
                    b.Status == BagStatus.LOCKED ||
                    b.Status == BagStatus.RESERVED)
            })
            .FirstOrDefaultAsync();

        if (location == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với mã #{id}.");
        }

        return location;
    }

    public async Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationRequest request, int userId, string? ipAddress)
    {
        var trimmedRefName = request.RefrigeratorName.Trim();
        var trimmedShelf = request.ShelfNumber.Trim();

        var isDuplicate = await _context.StorageLocations.AnyAsync(l =>
            l.RefrigeratorName.ToLower() == trimmedRefName.ToLower() &&
            l.ShelfNumber.ToLower() == trimmedShelf.ToLower());

        if (isDuplicate)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí ngăn '{trimmedShelf}' thuộc tủ '{trimmedRefName}' đã tồn tại trong hệ thống.");
        }

        var location = new StorageLocation
        {
            RefrigeratorName = trimmedRefName,
            ShelfNumber = trimmedShelf,
            TargetTemperature = request.TargetTemperature,
            IsFull = request.IsFull,
            IsActive = request.IsActive
        };

        _context.StorageLocations.Add(location);
        await _context.SaveChangesAsync();

        var auditLog = new AuditLog
        {
            TableName = "storage_locations",
            RecordId = location.LocationId,
            ActionType = ActionType.INSERT,
            NewState = JsonSerializer.Serialize(new
            {
                location.LocationId,
                location.RefrigeratorName,
                location.ShelfNumber,
                location.TargetTemperature,
                location.IsFull,
                location.IsActive
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        return new StorageLocationDto
        {
            LocationId = location.LocationId,
            RefrigeratorName = location.RefrigeratorName,
            ShelfNumber = location.ShelfNumber,
            TargetTemperature = location.TargetTemperature,
            IsFull = location.IsFull,
            IsActive = location.IsActive,
            CurrentBagsCount = 0
        };
    }

    public async Task<StorageLocationDto> UpdateStorageLocationAsync(int id, UpdateStorageLocationRequest request, int userId, string? ipAddress)
    {
        var location = await _context.StorageLocations.FirstOrDefaultAsync(l => l.LocationId == id);
        if (location == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với mã #{id}.");
        }

        var trimmedRefName = request.RefrigeratorName.Trim();
        var trimmedShelf = request.ShelfNumber.Trim();

        var isDuplicate = await _context.StorageLocations.AnyAsync(l =>
            l.LocationId != id &&
            l.RefrigeratorName.ToLower() == trimmedRefName.ToLower() &&
            l.ShelfNumber.ToLower() == trimmedShelf.ToLower());

        if (isDuplicate)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí ngăn '{trimmedShelf}' thuộc tủ '{trimmedRefName}' đã được sử dụng bởi vị trí khác.");
        }

        var oldState = JsonSerializer.Serialize(new
        {
            location.LocationId,
            location.RefrigeratorName,
            location.ShelfNumber,
            location.TargetTemperature,
            location.IsFull,
            location.IsActive
        });

        location.RefrigeratorName = trimmedRefName;
        location.ShelfNumber = trimmedShelf;
        location.TargetTemperature = request.TargetTemperature;
        location.IsFull = request.IsFull;
        location.IsActive = request.IsActive;

        var auditLog = new AuditLog
        {
            TableName = "storage_locations",
            RecordId = location.LocationId,
            ActionType = ActionType.UPDATE,
            OldState = oldState,
            NewState = JsonSerializer.Serialize(new
            {
                location.LocationId,
                location.RefrigeratorName,
                location.ShelfNumber,
                location.TargetTemperature,
                location.IsFull,
                location.IsActive
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        var currentBags = await _context.BloodBags.CountAsync(b =>
            b.LocationId == id &&
            (b.Status == BagStatus.AVAILABLE ||
             b.Status == BagStatus.READY_TESTING ||
             b.Status == BagStatus.TESTED_PASSED ||
             b.Status == BagStatus.LOCKED ||
             b.Status == BagStatus.RESERVED));

        return new StorageLocationDto
        {
            LocationId = location.LocationId,
            RefrigeratorName = location.RefrigeratorName,
            ShelfNumber = location.ShelfNumber,
            TargetTemperature = location.TargetTemperature,
            IsFull = location.IsFull,
            IsActive = location.IsActive,
            CurrentBagsCount = currentBags
        };
    }

    public async Task DeleteStorageLocationAsync(int id, int userId, string? ipAddress)
    {
        var location = await _context.StorageLocations.FirstOrDefaultAsync(l => l.LocationId == id);
        if (location == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với mã #{id}.");
        }

        var activeBagsCount = await _context.BloodBags.CountAsync(b =>
            b.LocationId == id &&
            (b.Status == BagStatus.AVAILABLE ||
             b.Status == BagStatus.READY_TESTING ||
             b.Status == BagStatus.TESTED_PASSED ||
             b.Status == BagStatus.LOCKED ||
             b.Status == BagStatus.RESERVED));

        if (activeBagsCount > 0)
        {
            throw new HttpException(
                StatusCodes.Status400BadRequest,
                $"Không thể xóa vị trí lưu trữ đang có {activeBagsCount} túi máu đang bảo quản. Vui lòng điều chuyển các túi máu sang vị trí khác trước khi xóa.");
        }

        var oldState = JsonSerializer.Serialize(new
        {
            location.LocationId,
            location.RefrigeratorName,
            location.ShelfNumber,
            location.TargetTemperature,
            location.IsFull,
            location.IsActive
        });

        var hasAnyReference = await _context.BloodBags.AnyAsync(b => b.LocationId == id);
        if (hasAnyReference)
        {
            location.IsActive = false;
        }
        else
        {
            _context.StorageLocations.Remove(location);
        }

        var auditLog = new AuditLog
        {
            TableName = "storage_locations",
            RecordId = id,
            ActionType = ActionType.DELETE,
            OldState = oldState,
            NewState = hasAnyReference ? "{\"status\":\"deactivated\"}" : "{\"status\":\"physically_deleted\"}",
            PerformedBy = userId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}
