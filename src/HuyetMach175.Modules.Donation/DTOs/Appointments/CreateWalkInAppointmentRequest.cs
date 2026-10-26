using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CreateWalkInAppointmentRequest
{
    [Required(ErrorMessage = "Mã người hiến máu (donorId) là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã người hiến máu phải lớn hơn 0.")]
    public int DonorId { get; set; }

    public int? CampaignId { get; set; }

    [MaxLength(20, ErrorMessage = "Khung giờ (timeSlot) không được vượt quá 20 ký tự.")]
    public string? TimeSlot { get; set; }
}

