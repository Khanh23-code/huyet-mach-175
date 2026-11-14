using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.DTOs.Common;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodReturnsService
{
    Task<CreateBloodReturnResponse> CreateBloodReturnAsync(CreateBloodReturnRequest request, int userId, string? ipAddress);
    Task<PagedResult<PendingBloodReturnDto>> GetPendingBloodReturnsAsync(PendingBloodReturnFilterDto filter, int userId);
    Task<ReturnInspectionDto> ScanBloodReturnByBarcodeAsync(string barcode, int userId);
    Task<ProcessBloodReturnResponse> ProcessBloodReturnAsync(int returnId, ProcessBloodReturnRequest request, int userId, string? ipAddress);
}
