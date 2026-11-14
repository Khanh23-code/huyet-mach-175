using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class AvailableBloodBagDto
{
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? LocationName { get; set; }
    public string BloodType { get; set; } = string.Empty;
    public BloodType BloodTypeCode { get; set; }
    public RhFactor RhFactor { get; set; }
    public bool IsExactMatch { get; set; }
    public bool IsCompatible { get; set; }
}
