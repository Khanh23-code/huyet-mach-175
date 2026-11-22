using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.DTOs.Inventory;

public class InventorySummaryFilterDto
{
    [FromQuery(Name = "bloodType")]
    public string? BloodType { get; set; }

    [FromQuery(Name = "rhFactor")]
    public string? RhFactor { get; set; }

    [FromQuery(Name = "componentType")]
    public string? ComponentType { get; set; }

    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "isExpiringSoon")]
    public bool? IsExpiringSoon { get; set; }

    [FromQuery(Name = "storageLocationId")]
    public int? StorageLocationId { get; set; }
}