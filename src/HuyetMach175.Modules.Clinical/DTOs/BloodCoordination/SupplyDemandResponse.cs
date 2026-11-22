using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodCoordination;

public class SupplyDemandResponse
{
    public int ComponentTypeId { get; set; }
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public int TotalAvailableStock { get; set; }
    public int TotalPendingDemand { get; set; }
    public List<string> CriticalShortages { get; set; } = new List<string>();
    public List<SupplyDemandDetailDto> Details { get; set; } = new List<SupplyDemandDetailDto>();
}
