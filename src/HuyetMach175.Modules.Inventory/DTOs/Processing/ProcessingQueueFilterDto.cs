using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.DTOs.Processing;

public class ProcessingQueueFilterDto
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
}