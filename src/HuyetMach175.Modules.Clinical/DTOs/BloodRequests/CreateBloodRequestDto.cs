using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class CreateBloodRequestDto
{
    [Required(ErrorMessage = "Mã người bệnh (patientCode) không được để trống.")]
    [MaxLength(50, ErrorMessage = "Mã người bệnh tối đa 50 ký tự.")]
    public string PatientCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên người bệnh (patientName) không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên người bệnh tối đa 100 ký tự.")]
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhóm máu người bệnh (patientBloodType) là bắt buộc.")]
    public BloodType PatientBloodType { get; set; }

    [Required(ErrorMessage = "Yếu tố Rh người bệnh (patientRh) là bắt buộc.")]
    public RhFactor PatientRh { get; set; }

    [Required(ErrorMessage = "Mức độ khẩn cấp (urgencyLevel) là bắt buộc.")]
    public UrgencyLevel UrgencyLevel { get; set; }

    [MaxLength(255, ErrorMessage = "Chẩn đoán tối đa 255 ký tự.")]
    public string? Diagnosis { get; set; }

    public bool IsDraft { get; set; }

    [Required(ErrorMessage = "Danh sách chế phẩm máu yêu cầu không được để trống.")]
    [MinLength(1, ErrorMessage = "Phiếu yêu cầu phải có ít nhất 1 chế phẩm máu.")]
    public List<CreateBloodRequestItemDto> Items { get; set; } = new List<CreateBloodRequestItemDto>();
}
