using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodComponentTypes;

public class BloodComponentTypeDto
{
    [JsonPropertyName("componentTypeId")]
    public int ComponentTypeId { get; set; }

    [JsonPropertyName("typeCode")]
    public ComponentCode TypeCode { get; set; }

    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;

    [JsonPropertyName("lifeDays")]
    public int LifeDays { get; set; }

    [JsonPropertyName("storageTemperature")]
    public string StorageTemperature { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("minTemp")]
    public decimal MinTemp { get; set; }

    [JsonPropertyName("maxTemp")]
    public decimal MaxTemp { get; set; }

    [JsonPropertyName("allowRestock")]
    public bool AllowRestock { get; set; }

    [JsonPropertyName("maxOutsideMinutesForRestock")]
    public int? MaxOutsideMinutesForRestock { get; set; }

    [JsonPropertyName("maxRestockCount")]
    public int MaxRestockCount { get; set; }

    [JsonPropertyName("compatibilityRuleType")]
    public CompatibilityRuleType CompatibilityRuleType { get; set; }
}

public class UpdateBloodComponentTypeRequest
{
    [Required(ErrorMessage = "Tên chế phẩm máu không được để trống.")]
    [MaxLength(100, ErrorMessage = "Tên chế phẩm máu tối đa 100 ký tự.")]
    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;

    [Range(1, 3650, ErrorMessage = "Hạn sử dụng (số ngày) phải từ 1 đến 3650 ngày.")]
    [JsonPropertyName("lifeDays")]
    public int LifeDays { get; set; }

    [Required(ErrorMessage = "Nhãn dải nhiệt độ bảo quản không được để trống.")]
    [MaxLength(50, ErrorMessage = "Nhãn nhiệt độ bảo quản tối đa 50 ký tự.")]
    [JsonPropertyName("storageTemperature")]
    public string StorageTemperature { get; set; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Mô tả tối đa 255 ký tự.")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [Range(-100.0, 50.0, ErrorMessage = "Nhiệt độ tối thiểu phải nằm trong dải từ -100°C đến 50°C.")]
    [JsonPropertyName("minTemp")]
    public decimal MinTemp { get; set; }

    [Range(-100.0, 50.0, ErrorMessage = "Nhiệt độ tối đa phải nằm trong dải từ -100°C đến 50°C.")]
    [JsonPropertyName("maxTemp")]
    public decimal MaxTemp { get; set; }

    [JsonPropertyName("allowRestock")]
    public bool AllowRestock { get; set; }

    [JsonPropertyName("maxOutsideMinutesForRestock")]
    public int? MaxOutsideMinutesForRestock { get; set; }

    [Range(0, 100, ErrorMessage = "Số lần tái nhập kho tối đa phải từ 0 đến 100.")]
    [JsonPropertyName("maxRestockCount")]
    public int MaxRestockCount { get; set; }

    [EnumDataType(typeof(CompatibilityRuleType), ErrorMessage = "Quy tắc hòa hợp miễn dịch không hợp lệ (RBC, PLASMA, PLATELET).")]
    [JsonPropertyName("compatibilityRuleType")]
    public CompatibilityRuleType CompatibilityRuleType { get; set; }
}
