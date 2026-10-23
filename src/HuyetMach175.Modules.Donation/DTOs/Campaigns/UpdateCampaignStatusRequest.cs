using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Campaigns;

public class UpdateCampaignStatusRequest
{
    [Required(ErrorMessage = "Trạng thái mới không được để trống.")]
    public CampaignStatus Status { get; set; }
}