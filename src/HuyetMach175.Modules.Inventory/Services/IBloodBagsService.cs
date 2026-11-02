using HuyetMach175.Modules.Inventory.DTOs.BloodBags;

namespace HuyetMach175.Modules.Inventory.Services;

public interface IBloodBagsService
{
    Task<ReceiveBloodBagResponse> ReceiveBloodBagAsync(ReceiveBloodBagRequest request, int userId, string? ipAddress);
}