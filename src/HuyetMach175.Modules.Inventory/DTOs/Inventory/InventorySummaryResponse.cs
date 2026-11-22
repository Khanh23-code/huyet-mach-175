using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Inventory;

public class InventorySummaryResponse
{
    [JsonPropertyName("summaryDate")]
    public DateTime SummaryDate { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("overview")]
    public InventoryOverviewDto Overview { get; set; } = new InventoryOverviewDto();

    [JsonPropertyName("breakdown")]
    public List<InventoryBreakdownItemDto> Breakdown { get; set; } = new List<InventoryBreakdownItemDto>();
}

public class InventoryOverviewDto
{
    [JsonPropertyName("totalBags")]
    public int TotalBags { get; set; }

    [JsonPropertyName("totalVolumeMl")]
    public int TotalVolumeMl { get; set; }

    [JsonPropertyName("availableBags")]
    public int AvailableBags { get; set; }

    [JsonPropertyName("expiringSoonBags")]
    public int ExpiringSoonBags { get; set; }

    [JsonPropertyName("disposalPendingBags")]
    public int DisposalPendingBags { get; set; }

    [JsonPropertyName("testingOrProcessingBags")]
    public int TestingOrProcessingBags { get; set; }
}

public class InventoryBreakdownItemDto
{
    [JsonPropertyName("bloodType")]
    public string BloodType { get; set; } = string.Empty;

    [JsonPropertyName("rhFactor")]
    public string RhFactor { get; set; } = string.Empty;

    [JsonPropertyName("bloodGroup")]
    public string BloodGroup { get; set; } = string.Empty;

    [JsonPropertyName("componentType")]
    public string ComponentType { get; set; } = string.Empty;

    [JsonPropertyName("componentTypeName")]
    public string? ComponentTypeName { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalBags")]
    public int TotalBags { get; set; }

    [JsonPropertyName("totalVolumeMl")]
    public int TotalVolumeMl { get; set; }
}