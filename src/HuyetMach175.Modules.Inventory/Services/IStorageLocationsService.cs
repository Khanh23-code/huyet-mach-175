using HuyetMach175.Modules.Inventory.DTOs.StorageLocations;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IStorageLocationsService
{
    Task<List<StorageLocationDto>> GetStorageLocationsAsync(bool? availableOnly = null);
    Task<StorageLocationDto> GetStorageLocationByIdAsync(int id);
    Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationRequest request, int userId, string? ipAddress);
    Task<StorageLocationDto> UpdateStorageLocationAsync(int id, UpdateStorageLocationRequest request, int userId, string? ipAddress);
    Task DeleteStorageLocationAsync(int id, int userId, string? ipAddress);
}