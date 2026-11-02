using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodBags;

public class ReceiveBloodBagRequest
{
    [Required(ErrorMessage = "Mã barcode túi máu là bắt buộc.")]
    [MaxLength(50, ErrorMessage = "Mã barcode không vượt quá 50 ký tự.")]
    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhiệt độ tiếp nhận là bắt buộc.")]
    [JsonPropertyName("receivedTemperature")]
    public decimal ReceivedTemperature { get; set; }

    [Required(ErrorMessage = "Vị trí lưu trữ là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ID vị trí lưu trữ không hợp lệ.")]
    [JsonPropertyName("storageLocationId")]
    public int StorageLocationId { get; set; }
}