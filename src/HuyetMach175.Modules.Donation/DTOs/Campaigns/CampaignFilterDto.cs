using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class CampaignFilterDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Số trang (PageNumber) phải từ 1 trở lên.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Kích thước trang (PageSize) phải từ 1 đến 100.")]
    public int PageSize { get; set; } = 10;

    public CampaignStatus? Status { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    [MaxLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự.")]
    public string? Keyword { get; set; }
}