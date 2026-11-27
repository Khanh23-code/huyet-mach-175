using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.DTOs.Common;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodReturnsService
{
    Task<CreateBloodReturnResponse> CreateBloodReturnAsync(CreateBloodReturnRequest request, Guid userId, string? ipAddress);
    Task<CancelBloodReturnResponse> CancelBloodReturnAsync(Guid returnId, CancelBloodReturnRequest? request, Guid userId, string? ipAddress);
    Task<PagedResult<PendingBloodReturnDto>> GetPendingBloodReturnsAsync(PendingBloodReturnFilterDto filter, Guid userId);
    Task<ReturnInspectionDto> ScanBloodReturnByBarcodeAsync(string barcode, Guid userId);
    Task<ProcessBloodReturnResponse> ProcessBloodReturnAsync(Guid returnId, ProcessBloodReturnRequest request, Guid userId, string? ipAddress);
}
