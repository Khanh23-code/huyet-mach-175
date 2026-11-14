using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class BloodCoordinationQueueFilterDto
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "urgencyLevel")]
    public string? UrgencyLevel { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
}
