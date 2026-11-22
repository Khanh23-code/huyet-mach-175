using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class RegisterExternalBloodBagRequest
{
    [Required(ErrorMessage = "Đơn vị cung cấp là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên đơn vị cung cấp không vượt quá 150 ký tự.")]
    [JsonPropertyName("externalUnitName")]
    public string ExternalUnitName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã túi của đơn vị ngoài là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Mã túi ngoài không vượt quá 100 ký tự.")]
    [JsonPropertyName("externalCode")]
    public string ExternalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhóm máu ABO là bắt buộc.")]
    [JsonPropertyName("bloodType")]
    public BloodType? BloodType { get; set; }

    [Required(ErrorMessage = "Yếu tố Rh là bắt buộc.")]
    [JsonPropertyName("rhFactor")]
    public RhFactor? RhFactor { get; set; }

    [Range(100, 600, ErrorMessage = "Thể tích phải từ 100 đến 600 ml.")]
    [JsonPropertyName("volumeMl")]
    public int VolumeMl { get; set; }

    [Required(ErrorMessage = "Thời gian thu thập là bắt buộc.")]
    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [Required(ErrorMessage = "Nhiệt độ tiếp nhận là bắt buộc.")]
    [JsonPropertyName("receivedTemperature")]
    public decimal ReceivedTemperature { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ID vị trí lưu trữ không hợp lệ.")]
    [JsonPropertyName("storageLocationId")]
    public int StorageLocationId { get; set; }
}