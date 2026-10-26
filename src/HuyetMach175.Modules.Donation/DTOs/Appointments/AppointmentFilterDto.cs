using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class AppointmentFilterDto
{
    [FromQuery(Name = "date")]
    public DateOnly? Date { get; set; }

    [FromQuery(Name = "campaignId")]
    public int? CampaignId { get; set; }

    [FromQuery(Name = "status")]
    public AppointmentStatus? Status { get; set; }

    [FromQuery(Name = "timeSlot")]
    [MaxLength(20, ErrorMessage = "Khung giờ (timeSlot) không được vượt quá 20 ký tự.")]
    public string? TimeSlot { get; set; }

    [FromQuery(Name = "isWalkIn")]
    public bool? IsWalkIn { get; set; }

    [FromQuery(Name = "search")]
    [MaxLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự.")]
    public string? Search { get; set; }

    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Số trang (page) phải lớn hơn hoặc bằng 1.")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    [Range(1, 100, ErrorMessage = "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.")]
    public int PageSize { get; set; } = 20;
}

