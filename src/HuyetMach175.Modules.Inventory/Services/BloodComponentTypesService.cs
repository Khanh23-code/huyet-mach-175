using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodComponentTypes;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class BloodComponentTypesService : IBloodComponentTypesService
{
    private readonly ApplicationDbContext _context;

    public BloodComponentTypesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BloodComponentTypeDto>> GetAllAsync()
    {
        return await _context.BloodComponentTypes
            .AsNoTracking()
            .OrderBy(c => c.ComponentTypeId)
            .Select(c => new BloodComponentTypeDto
            {
                ComponentTypeId = c.ComponentTypeId,
                TypeCode = c.TypeCode,
                TypeName = c.TypeName,
                LifeDays = c.LifeDays,
                StorageTemperature = c.StorageTemperature,
                Description = c.Description,
                MinTemp = c.MinTemp,
                MaxTemp = c.MaxTemp,
                AllowRestock = c.AllowRestock,
                MaxOutsideMinutesForRestock = c.MaxOutsideMinutesForRestock,
                MaxRestockCount = c.MaxRestockCount,
                CompatibilityRuleType = c.CompatibilityRuleType
            })
            .ToListAsync();
    }

    public async Task<BloodComponentTypeDto> GetByIdAsync(int id)
    {
        var item = await _context.BloodComponentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ComponentTypeId == id);

        if (item == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy loại chế phẩm máu với mã #{id}.");
        }

        return ToDto(item);
    }

    public async Task<BloodComponentTypeDto> UpdateAsync(int id, UpdateBloodComponentTypeRequest request, int userId, string? ipAddress)
    {
        if (request.MinTemp > request.MaxTemp)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Nhiệt độ bảo quản tối thiểu không được lớn hơn nhiệt độ tối đa.");
        }

        if (request.MaxOutsideMinutesForRestock.HasValue && request.MaxOutsideMinutesForRestock.Value <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Thời gian ngoài tủ tối đa cho phép tái nhập kho phải lớn hơn 0 phút.");
        }

        var trimmedName = request.TypeName.Trim();
        var trimmedStorageTemp = request.StorageTemperature.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName) || string.IsNullOrWhiteSpace(trimmedStorageTemp))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Tên chế phẩm máu và nhãn nhiệt độ bảo quản không được để trống.");
        }

        var item = await _context.BloodComponentTypes.FirstOrDefaultAsync(c => c.ComponentTypeId == id);
        if (item == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy loại chế phẩm máu với mã #{id}.");
        }

        var oldState = SerializeState(item);

        item.TypeName = trimmedName;
        item.LifeDays = request.LifeDays;
        item.StorageTemperature = trimmedStorageTemp;
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        item.MinTemp = request.MinTemp;
        item.MaxTemp = request.MaxTemp;
        item.AllowRestock = request.AllowRestock;
        item.MaxOutsideMinutesForRestock = request.MaxOutsideMinutesForRestock;
        item.MaxRestockCount = request.MaxRestockCount;
        item.CompatibilityRuleType = request.CompatibilityRuleType;

        _context.AuditLogs.Add(new AuditLog
        {
            TableName = "blood_component_types",
            RecordId = item.ComponentTypeId,
            ActionType = ActionType.UPDATE,
            OldState = oldState,
            NewState = SerializeState(item),
            PerformedBy = userId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return ToDto(item);
    }

    private static BloodComponentTypeDto ToDto(BloodComponentType item) => new()
    {
        ComponentTypeId = item.ComponentTypeId,
        TypeCode = item.TypeCode,
        TypeName = item.TypeName,
        LifeDays = item.LifeDays,
        StorageTemperature = item.StorageTemperature,
        Description = item.Description,
        MinTemp = item.MinTemp,
        MaxTemp = item.MaxTemp,
        AllowRestock = item.AllowRestock,
        MaxOutsideMinutesForRestock = item.MaxOutsideMinutesForRestock,
        MaxRestockCount = item.MaxRestockCount,
        CompatibilityRuleType = item.CompatibilityRuleType
    };

    private static string SerializeState(BloodComponentType item) => JsonSerializer.Serialize(new
    {
        item.ComponentTypeId,
        TypeCode = item.TypeCode.ToString(),
        item.TypeName,
        item.LifeDays,
        item.StorageTemperature,
        item.Description,
        item.MinTemp,
        item.MaxTemp,
        item.AllowRestock,
        item.MaxOutsideMinutesForRestock,
        item.MaxRestockCount,
        CompatibilityRuleType = item.CompatibilityRuleType.ToString()
    });
}
