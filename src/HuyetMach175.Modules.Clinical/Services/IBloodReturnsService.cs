using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodReturnsService
{
    Task<CreateBloodReturnResponse> CreateBloodReturnAsync(CreateBloodReturnRequest request, int userId, string? ipAddress);
}
