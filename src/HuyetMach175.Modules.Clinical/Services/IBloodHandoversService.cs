using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodHandoversService
{
    Task<HandoverSessionDto> CreateHandoverSessionAsync(CreateHandoverSessionRequest request, int userId, string? ipAddress);
    Task<List<HandoverSessionDto>> GetPendingHandoverSessionsAsync(PendingHandoverSessionFilterDto filter, int userId);
    Task<HandoverSessionDto> GetHandoverSessionByIdAsync(int sessionId, int userId);
    Task<HandoverSessionDto> ConfirmHandoverSessionAsync(int sessionId, ConfirmHandoverSessionRequest request, int userId, string? ipAddress);
    Task<HandoverSessionDto> CancelHandoverSessionAsync(int sessionId, CancelHandoverSessionRequest? request, int userId, string? ipAddress);
}
