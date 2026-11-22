using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class SubmitBloodRequestResponse
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public DateTime SubmittedAt { get; set; }
}
