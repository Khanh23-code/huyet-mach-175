using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.StorageLocations;

public class CreateStorageLocationRequest
{
    [Required(ErrorMessage = "Tên tủ bảo quản không được để trống.")]
    [MaxLength(50, ErrorMessage = "Tên tủ bảo quản tối đa 50 ký tự.")]
    [JsonPropertyName("refrigeratorName")]
    public string RefrigeratorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số hoặc mã ngăn/kệ không được để trống.")]
    [MaxLength(20, ErrorMessage = "Mã ngăn/kệ tối đa 20 ký tự.")]
    [JsonPropertyName("shelfNumber")]
    public string ShelfNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhiệt độ cài đặt mục tiêu là bắt buộc.")]
    [Range(-100.0, 50.0, ErrorMessage = "Nhiệt độ mục tiêu phải nằm trong dải từ -100°C đến 50°C.")]
    [JsonPropertyName("targetTemperature")]
    public decimal TargetTemperature { get; set; }

    [JsonPropertyName("isFull")]
    public bool IsFull { get; set; } = false;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}