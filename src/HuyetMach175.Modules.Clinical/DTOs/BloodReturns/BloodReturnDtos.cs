using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodReturns;

public class BloodReturnChecklistDto
{
    public bool IsUnusedFullVolume { get; set; }
    public bool IsBagIntact { get; set; }
    public bool IsLabelClear { get; set; }
    public bool IsStoredUnder30Mins { get; set; }
}

public class CreateBloodReturnRequest
{
    [Required(ErrorMessage = "Mã phân bổ (allocationId) là bắt buộc.")]
    public int AllocationId { get; set; }

    [Required(ErrorMessage = "Lý do hoàn trả (returnReason) là bắt buộc.")]
    [MaxLength(255, ErrorMessage = "Lý do hoàn trả tối đa 255 ký tự.")]
    public string ReturnReason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bảng kiểm điều kiện hoàn trả (checklist) là bắt buộc.")]
    public BloodReturnChecklistDto Checklist { get; set; } = new BloodReturnChecklistDto();
}

public class CreateBloodReturnResponse
{
    public int ReturnId { get; set; }
    public int AllocationId { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string ReturnReason { get; set; } = string.Empty;
    public bool Qualified { get; set; }
    public ReturnFinalAction FinalAction { get; set; }
    public BagStatus BagStatus { get; set; }
    public int ReturnedById { get; set; }
    public string? ReturnedByName { get; set; }
    public DateTime ProcessedAt { get; set; }
}
