using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Donation.DTOs.Sessions;

public class UpdateCollectionRequest
{
    [JsonPropertyName("actualVolumeMl")]
    [Range(50, 1000, ErrorMessage = "Thể tích máu thực tế phải nằm trong khoảng từ 50ml đến 1000ml.")]
    public int? ActualVolumeMl { get; set; }

    [JsonPropertyName("collectionIncident")]
    [MaxLength(500, ErrorMessage = "Ghi chú sự cố không được vượt quá 500 ký tự.")]
    public string? CollectionIncident { get; set; }

    [Required(ErrorMessage = "Trạng thái phiên lấy máu (status) là bắt buộc.")]
    [JsonPropertyName("status")]
    public SessionStatus Status { get; set; }
}