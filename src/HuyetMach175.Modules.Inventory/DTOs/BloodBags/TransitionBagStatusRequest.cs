using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class TransitionBagStatusRequest
{
    [Required(ErrorMessage = "Mã vạch túi máu (barcode) là bắt buộc.")]
    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trạng thái đích (targetStatus) là bắt buộc.")]
    [JsonPropertyName("targetStatus")]
    public BagStatus TargetStatus { get; set; }

    [JsonPropertyName("storageLocationId")]
    public int? StorageLocationId { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

