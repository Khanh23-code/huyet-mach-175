using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "Mã người hiến máu (donorId) là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã người hiến máu phải lớn hơn 0.")]
    public int DonorId { get; set; }

    [Required(ErrorMessage = "Mã khung giờ chiến dịch (slotId) là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Mã khung giờ (slotId) phải lớn hơn 0.")]
    public int SlotId { get; set; }

    [Required(ErrorMessage = "Ngày hẹn (appointmentDate) là bắt buộc.")]
    public DateOnly AppointmentDate { get; set; }
}
