using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.DTOs.Inventory;

public class ExpiringSoonFilterDto
{
    [FromQuery(Name = "daysThreshold")]
    public int DaysThreshold { get; set; } = 3;

    [FromQuery(Name = "bloodType")]
    public string? BloodType { get; set; }

    [FromQuery(Name = "rhFactor")]
    public string? RhFactor { get; set; }

    [FromQuery(Name = "componentType")]
    public string? ComponentType { get; set; }

    [FromQuery(Name = "storageLocationId")]
    public int? StorageLocationId { get; set; }

    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "page")]
    public int? Page { set { if (value.HasValue) PageNumber = value.Value; } }

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
}

