namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class SupplyDemandDetailDto
{
    public string BloodType { get; set; } = string.Empty;
    public int StockUnits { get; set; }
    public int DemandUnits { get; set; }
    public bool IsShortage { get; set; }
    public int ShortageUnits { get; set; }
}
