using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.DTOs.StorageLocations;

public class StorageLocationFilterDto
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "isFull")]
    public bool? IsFull { get; set; }

    [FromQuery(Name = "isActive")]
    public bool? IsActive { get; set; }

    [FromQuery(Name = "availableOnly")]
    public bool? AvailableOnly { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "page")]
    public int? Page { set { if (value.HasValue) PageNumber = value.Value; } }

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
}