using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.Processing;

public class ComponentTypeOptionDto
{
    [JsonPropertyName("componentTypeId")] public int ComponentTypeId { get; set; }
    [JsonPropertyName("typeCode")] public string TypeCode { get; set; } = string.Empty;
    [JsonPropertyName("typeName")] public string TypeName { get; set; } = string.Empty;
    [JsonPropertyName("lifeDays")] public int LifeDays { get; set; }
    [JsonPropertyName("storageTemperature")] public string StorageTemperature { get; set; } = string.Empty;
}