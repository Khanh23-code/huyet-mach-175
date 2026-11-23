using HuyetMach175.Modules.Inventory.DTOs.BloodComponentTypes;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodComponentTypesService
{
    Task<List<BloodComponentTypeDto>> GetAllAsync();
    Task<BloodComponentTypeDto> GetByIdAsync(int id);
    Task<BloodComponentTypeDto> UpdateAsync(int id, UpdateBloodComponentTypeRequest request, int userId, string? ipAddress);
}
