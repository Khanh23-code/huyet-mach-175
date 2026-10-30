using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CreateWalkInAppointmentRequest
{
    [Required(ErrorMessage = "Mã người hiến máu (donorId) là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã người hiến máu phải lớn hơn 0.")]
    public int DonorId { get; set; }

    public int? SlotId { get; set; }

    public int? CampaignId { get; set; }
}