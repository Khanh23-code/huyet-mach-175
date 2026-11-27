using HuyetMach175.Modules.Inventory.DTOs.BloodComponentTypes;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodComponentTypesService
{
    Task<List<BloodComponentTypeDto>> GetAllAsync();
    Task<BloodComponentTypeDto> GetByIdAsync(Guid id);
    Task<BloodComponentTypeDto> UpdateAsync(Guid id, UpdateBloodComponentTypeRequest request, Guid userId, string? ipAddress);
}
