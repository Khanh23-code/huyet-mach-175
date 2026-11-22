using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Inventory;

public class RelocateBloodBagRequest
{
    [Required(ErrorMessage = "ID vị trí lưu trữ mới là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ID vị trí lưu trữ mới không hợp lệ.")]
    [JsonPropertyName("newStorageLocationId")]
    public int NewStorageLocationId { get; set; }

    [MaxLength(255, ErrorMessage = "Ghi chú điều chuyển tối đa 255 ký tự.")]
    [JsonPropertyName("note")]
    public string? Note { get; set; }
}