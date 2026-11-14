using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class RejectBloodRequestResponse
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public int ReleasedBagCount { get; set; }
    public DateTime RejectedAt { get; set; }
}
