using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class BloodRequestItemDetailDto
{
    public int RequestItemId { get; set; }
    public int ComponentTypeId { get; set; }
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public int RequestedUnits { get; set; }
    public int AllocatedUnits { get; set; }
}
