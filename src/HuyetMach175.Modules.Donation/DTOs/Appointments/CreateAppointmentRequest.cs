using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "Mã người hiến máu (donorId) là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã người hiến máu phải lớn hơn 0.")]
    public int DonorId { get; set; }

    public int? CampaignId { get; set; }

    public int? SlotId { get; set; }

    [Required(ErrorMessage = "Ngày hẹn (appointmentDate) là bắt buộc.")]
    public DateOnly AppointmentDate { get; set; }

    [Required(ErrorMessage = "Khung giờ hẹn (timeSlot) là bắt buộc.")]
    [MaxLength(20, ErrorMessage = "Khung giờ hẹn không được vượt quá 20 ký tự.")]
    public string TimeSlot { get; set; } = string.Empty;
}
