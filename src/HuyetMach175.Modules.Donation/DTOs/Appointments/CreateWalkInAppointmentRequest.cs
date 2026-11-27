using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CreateWalkInAppointmentRequest
{
    [Required(ErrorMessage = "Mã người hiến máu (donorId) là bắt buộc.")]
    public Guid DonorId { get; set; }

    public Guid? SlotId { get; set; }

    public Guid? CampaignId { get; set; }
}