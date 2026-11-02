using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Processing;

public class SeparateBloodBagRequest
{
    [JsonPropertyName("parentBagId")]
    public string? ParentBagId { get; set; }

    [JsonPropertyName("parentBarcode")]
    public string? ParentBarcode { get; set; }

    [Required(ErrorMessage = "Danh sách chế phẩm con cần phân tách là bắt buộc.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất một chế phẩm con được tạo ra.")]
    [JsonPropertyName("components")]
    public List<ComponentSeparationItemDto> Components { get; set; } = new List<ComponentSeparationItemDto>();
}

public class ComponentSeparationItemDto
{
    [Required(ErrorMessage = "Loại chế phẩm là bắt buộc.")]
    [JsonPropertyName("componentType")]
    public string ComponentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thể tích chế phẩm là bắt buộc.")]
    [Range(1, 1000, ErrorMessage = "Thể tích chế phẩm phải lớn hơn 0 và nhỏ hơn 1000ml.")]
    [JsonPropertyName("volumeMl")]
    public int VolumeMl { get; set; }

    [Required(ErrorMessage = "Vị trí lưu trữ cho chế phẩm là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ID vị trí lưu trữ không hợp lệ.")]
    [JsonPropertyName("storageLocationId")]
    public int StorageLocationId { get; set; }
}