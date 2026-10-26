using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Appointments;

public class CancelAppointmentRequest
{
    public AppointmentStatus? Status { get; set; }

    [MaxLength(255, ErrorMessage = "Lý do hủy không được vượt quá 255 ký tự.")]
    public string? Reason { get; set; }
}

