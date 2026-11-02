using HuyetMach175.Modules.Inventory.DTOs.StorageLocations;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IStorageLocationsService
{
    Task<List<StorageLocationDto>> GetStorageLocationsAsync(bool? availableOnly = null);
}