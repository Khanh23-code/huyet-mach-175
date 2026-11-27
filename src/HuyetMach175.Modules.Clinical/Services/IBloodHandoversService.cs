using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;

namespace HuyetMach175.Modules.Clinical.Services;

public interface IBloodHandoversService
{
    Task<HandoverSessionDto> CreateHandoverSessionAsync(CreateHandoverSessionRequest request, Guid userId, string? ipAddress);
    Task<List<ReservedHandoverRequestDto>> GetReservedRequestsForHandoverAsync(string? search, string? urgencyLevel);
    Task<List<HandoverSessionDto>> GetPendingHandoverSessionsAsync(PendingHandoverSessionFilterDto filter, Guid userId);
    Task<HandoverSessionDto> GetHandoverSessionByIdAsync(Guid sessionId, Guid userId);
    Task<HandoverSessionDto> ConfirmHandoverSessionAsync(Guid sessionId, ConfirmHandoverSessionRequest request, Guid userId, string? ipAddress);
    Task<HandoverSessionDto> CancelHandoverSessionAsync(Guid sessionId, CancelHandoverSessionRequest? request, Guid userId, string? ipAddress);
    Task<TransfusionScanDto> ScanBloodBagForTransfusionAsync(string barcode, Guid userId);
    Task<ConfirmTransfusionResponse> ConfirmTransfusionAsync(ConfirmTransfusionRequest request, Guid userId, string? ipAddress);
}
