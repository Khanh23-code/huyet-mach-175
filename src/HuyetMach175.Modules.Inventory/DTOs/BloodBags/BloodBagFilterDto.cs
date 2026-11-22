using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class BloodBagFilterDto
{
    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "bloodType")]
    public string? BloodType { get; set; }

    [FromQuery(Name = "rhFactor")]
    public string? RhFactor { get; set; }

    [FromQuery(Name = "componentType")]
    public string? ComponentType { get; set; }

    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "locationId")]
    public int? LocationId { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
}

