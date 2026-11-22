using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class DisposalFilterDto
{
    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "reason")]
    public string? Reason { get; set; }

    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "fromDate")]
    public DateTime? FromDate { get; set; }

    [FromQuery(Name = "toDate")]
    public DateTime? ToDate { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
}