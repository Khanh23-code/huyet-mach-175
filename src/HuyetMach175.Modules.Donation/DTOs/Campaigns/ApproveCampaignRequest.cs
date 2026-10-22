using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class ApproveCampaignRequest
{
    [Required(ErrorMessage = "Trạng thái phê duyệt (IsApproved) không được để trống.")]
    public bool IsApproved { get; set; }

    [StringLength(255, ErrorMessage = "Lý do từ chối không được vượt quá 255 ký tự.")]
    public string? RejectionReason { get; set; }
}