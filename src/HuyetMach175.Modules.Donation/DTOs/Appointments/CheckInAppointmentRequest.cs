using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CheckInAppointmentRequest
{
    [Required(ErrorMessage = "Mã token QR (qrCodeToken) là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Mã token QR không được vượt quá 100 ký tự.")]
    public string QrCodeToken { get; set; } = string.Empty;
}

