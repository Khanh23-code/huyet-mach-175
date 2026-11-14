namespace HuyetMach175.Modules.Clinical.DTOs.BloodCoordination;

public class AllocatedBagSummaryDto
{
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExactMatch { get; set; }
}
