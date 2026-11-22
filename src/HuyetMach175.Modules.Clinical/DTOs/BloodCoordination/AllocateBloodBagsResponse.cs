using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodCoordination;

public class AllocateBloodBagsResponse
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int RequestItemId { get; set; }
    public int AllocatedCount { get; set; }
    public int ItemAllocatedUnits { get; set; }
    public int ItemRequestedUnits { get; set; }
    public RequestStatus RequestStatus { get; set; }
    public int CurrentStep { get; set; }
    public List<AllocatedBagSummaryDto> AllocatedBags { get; set; } = new List<AllocatedBagSummaryDto>();
}
