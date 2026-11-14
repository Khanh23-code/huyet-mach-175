using System.ComponentModel.DataAnnotations;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class RejectBloodRequestDto
{
    [Required(ErrorMessage = "Lý do từ chối (rejectionReason) không được để trống.")]
    [MaxLength(255, ErrorMessage = "Lý do từ chối tối đa 255 ký tự.")]
    public string RejectionReason { get; set; } = string.Empty;
}
