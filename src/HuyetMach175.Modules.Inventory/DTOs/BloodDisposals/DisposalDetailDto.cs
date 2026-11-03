using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class DisposalDetailDto : DisposalRecordDto
{
    [JsonPropertyName("items")]
    public List<DisposalItemDto> Items { get; set; } = new();
}
