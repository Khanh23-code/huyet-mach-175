using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class UpdateCampaignRequest
{
    [Required(ErrorMessage = "Tên đợt hiến máu không được để trống.")]
    [StringLength(150, ErrorMessage = "Tên đợt hiến máu không được vượt quá 150 ký tự.")]
    public string CampaignName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Địa điểm tổ chức không được để trống.")]
    [StringLength(255, ErrorMessage = "Địa điểm tổ chức không được vượt quá 255 ký tự.")]
    public string LocationName { get; set; } = string.Empty;

    public CampaignType CampaignType { get; set; } = CampaignType.EVENT;

    [MaxLength(50, ErrorMessage = "Ngày hoạt động không được vượt quá 50 ký tự.")]
    public string? OperatingDays { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
    public DateOnly EndDate { get; set; }

    [Range(1, 10000, ErrorMessage = "Chỉ tiêu thu gom phải từ 1 đến 10.000 túi máu.")]
    public int TargetDonations { get; set; } = 100;

    [Range(1, 20000, ErrorMessage = "Sức chứa tối đa phải lớn hơn 0.")]
    public int? MaxCapacity { get; set; }

    public List<CreateCampaignSlotDto> Slots { get; set; } = new List<CreateCampaignSlotDto>();
}
