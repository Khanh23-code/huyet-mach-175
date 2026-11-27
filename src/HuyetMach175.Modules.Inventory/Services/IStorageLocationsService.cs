using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.StorageLocations;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IStorageLocationsService
{
    Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(StorageLocationFilterDto filter);
    Task<StorageLocationDto> GetStorageLocationByIdAsync(Guid id);
    Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationRequest request, Guid userId, string? ipAddress);
    Task<StorageLocationDto> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationRequest request, Guid userId, string? ipAddress);
    Task DeleteStorageLocationAsync(Guid id, Guid userId, string? ipAddress);
}
