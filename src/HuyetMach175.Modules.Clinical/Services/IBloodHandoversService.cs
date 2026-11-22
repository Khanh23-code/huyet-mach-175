using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodHandoversService
{
    Task<DispatchBloodBagResponse> DispatchBagAsync(DispatchBloodBagRequest request, int userId, string? ipAddress);
    Task<List<PendingReceiptBagDto>> GetPendingClinicalReceiptsAsync(PendingClinicalReceiptFilterDto filter, int userId);
    Task<ConfirmReceiptResponse> ConfirmClinicalReceiptAsync(ConfirmReceiptRequest request, int userId, string? ipAddress);
}
