using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Donation.DTOs.Donors;

public class DonorFilterDto
{
    [FromQuery(Name = "search")]
    [MaxLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự.")]
    public string? Search { get; set; }

    [FromQuery(Name = "bloodType")]
    public BloodType? BloodType { get; set; }

    [FromQuery(Name = "rhFactor")]
    public RhFactor? RhFactor { get; set; }

    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Số trang (page) phải lớn hơn hoặc bằng 1.")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    [Range(1, 100, ErrorMessage = "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.")]
    public int PageSize { get; set; } = 10;
}