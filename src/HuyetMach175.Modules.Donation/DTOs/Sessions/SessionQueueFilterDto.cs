using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.DTOs.Sessions;

public class SessionQueueFilterDto
{
    [FromQuery(Name = "date")]
    public DateOnly? Date { get; set; }

    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "search")]
    [MaxLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự.")]
    public string? Search { get; set; }

    [FromQuery(Name = "pageNumber")]
    [Range(1, int.MaxValue, ErrorMessage = "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    [Range(1, 100, ErrorMessage = "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;
}
