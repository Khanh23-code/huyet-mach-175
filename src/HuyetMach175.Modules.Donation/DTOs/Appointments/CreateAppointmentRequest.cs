using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "Mã người hiến máu (donorId) là bắt buộc.")]
    public Guid DonorId { get; set; }

    [Required(ErrorMessage = "Mã khung giờ chiến dịch (slotId) là bắt buộc.")]
    public Guid SlotId { get; set; }

    [Required(ErrorMessage = "Ngày hẹn (appointmentDate) là bắt buộc.")]
    public DateOnly AppointmentDate { get; set; }
}
