using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class CancelCampaignRequest
{
    [Required(ErrorMessage = "Lý do hủy đợt hiến máu là bắt buộc.")]
    [MinLength(5, ErrorMessage = "Lý do hủy phải có ít nhất 5 ký tự.")]
    [StringLength(255, ErrorMessage = "Lý do hủy không được vượt quá 255 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

