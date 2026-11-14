using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;
using Microsoft.AspNetCore.Mvc;

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

public class PendingBloodReturnFilterDto
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
}

public class PendingBloodReturnDto
{
    public int ReturnId { get; set; }
    public int AllocationId { get; set; }
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime ExpiredAt { get; set; }
    public bool IsExpired { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int ReturnedById { get; set; }
    public string? ReturnedByName { get; set; }
    public string ReturnReason { get; set; } = string.Empty;
    public ReturnFinalAction FinalAction { get; set; }
    public BagStatus BagStatus { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class ReturnInspectionDto
{
    public int ReturnId { get; set; }
    public int AllocationId { get; set; }
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int ComponentTypeId { get; set; }
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime ExpiredAt { get; set; }
    public bool IsExpired { get; set; }
    public BagStatus BagStatus { get; set; }
    public int? PreviousLocationId { get; set; }
    public string? PreviousLocationName { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? DoctorName { get; set; }
    public int ReturnedById { get; set; }
    public string? ReturnedByName { get; set; }
    public string ReturnReason { get; set; } = string.Empty;
    public bool Qualified { get; set; }
    public ReturnFinalAction FinalAction { get; set; }
    public DateTime ReturnedAt { get; set; }
}

public class BloodReturnInspectionChecklistDto
{
    public bool IsBagIntact { get; set; }
    public bool IsLabelClear { get; set; }
    public bool IsTempInRange { get; set; }
    public bool IsOutsideTimeValid { get; set; }
    public bool IsUnusedFullVolume { get; set; }
}

public class ProcessBloodReturnRequest
{
    [Required(ErrorMessage = "Quyết định xử lý (action) là bắt buộc (RESTOCK hoặc DISCARD).")]
    public ReturnFinalAction Action { get; set; }

    public BloodReturnInspectionChecklistDto? Checklist { get; set; }

    public int? NewStorageLocationId { get; set; }

    [MaxLength(255, ErrorMessage = "Lý do tiêu hủy tối đa 255 ký tự.")]
    public string? DiscardReason { get; set; }
}

public class ProcessBloodReturnResponse
{
    public int ReturnId { get; set; }
    public int AllocationId { get; set; }
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public bool Qualified { get; set; }
    public ReturnFinalAction FinalAction { get; set; }
    public BagStatus BagStatus { get; set; }
    public AllocationStatus AllocationStatus { get; set; }
    public int? NewLocationId { get; set; }
    public string? NewLocationName { get; set; }
    public string? DiscardReason { get; set; }
    public int InspectedById { get; set; }
    public string? InspectedByName { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
