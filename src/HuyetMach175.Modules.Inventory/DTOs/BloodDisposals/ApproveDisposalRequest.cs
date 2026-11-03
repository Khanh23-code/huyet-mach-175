using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class ApproveDisposalRequest
{
    [Required(ErrorMessage = "Trạng thái phê duyệt (isApproved) là bắt buộc.")]
    [JsonPropertyName("isApproved")]
    public bool IsApproved { get; set; }

    [MaxLength(500, ErrorMessage = "Lý do từ chối không được vượt quá 500 ký tự.")]
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }
}

