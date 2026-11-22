using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;

public class CancelDisposalRequest
{
    [MaxLength(500, ErrorMessage = "Lý do rút đơn không được vượt quá 500 ký tự.")]
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

