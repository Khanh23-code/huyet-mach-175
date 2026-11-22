using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class CreateDisposalRequest
{
    [Required(ErrorMessage = "Danh sách mã vạch (barcode) túi máu cần tiêu hủy là bắt buộc.")]
    [MinLength(1, ErrorMessage = "Phải chọn tối thiểu 1 túi máu để lập biên bản tiêu hủy.")]
    [JsonPropertyName("barcodes")]
    public List<string> Barcodes { get; set; } = new();

    [Required(ErrorMessage = "Lý do đề xuất tiêu hủy là bắt buộc.")]
    [JsonPropertyName("reason")]
    public DisposalReason Reason { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}