using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class CreateDisposalRequest
{
    [JsonPropertyName("bagId")]
    public int? BagId { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "Lý do đề xuất tiêu hủy là bắt buộc.")]
    [JsonPropertyName("reason")]
    public DisposalReason Reason { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

