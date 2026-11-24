using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;

public class PendingHandoverSessionFilterDto
{
    [FromQuery(Name = "sessionCode")]
    public string? SessionCode { get; set; }

    [FromQuery(Name = "requestCode")]
    public string? RequestCode { get; set; }

    [FromQuery(Name = "doctorId")]
    public int? DoctorId { get; set; }

    [FromQuery(Name = "doctorName")]
    public string? DoctorName { get; set; }
}

public class CreateHandoverSessionRequest
{
    [Required(ErrorMessage = "Danh sách mã vạch túi máu (barcodes) là bắt buộc.")]
    [MinLength(1, ErrorMessage = "Phiên bàn giao phải có ít nhất 1 túi máu.")]
    public List<string> Barcodes { get; set; } = new List<string>();

    public int? RequestId { get; set; }

    [Range(3, 5, ErrorMessage = "Thời gian chờ xác nhận (timeoutMinutes) phải nằm trong khoảng 3 đến 5 phút.")]
    public int TimeoutMinutes { get; set; } = 5;
}

public class ConfirmHandoverSessionRequest
{
    [Required(ErrorMessage = "Danh sách mã vạch quét xác nhận (scannedBarcodes) là bắt buộc.")]
    [MinLength(1, ErrorMessage = "Phải quét ít nhất 1 mã vạch túi máu để xác nhận bàn giao.")]
    public List<string> ScannedBarcodes { get; set; } = new List<string>();

    public string? ExpectedPatientCode { get; set; }
}

public class CancelHandoverSessionRequest
{
    [MaxLength(255, ErrorMessage = "Lý do hủy phiên tối đa 255 ký tự.")]
    public string? CancelReason { get; set; }
}

public class HandoverSessionItemDto
{
    public int AllocationId { get; set; }
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int ComponentTypeId { get; set; }
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime ExpiredAt { get; set; }
    public BagStatus BagStatus { get; set; }
    public string? LocationName { get; set; }
}

public class HandoverSessionDto
{
    public int HandoverSessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public HandoverSessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RemainingSeconds { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public int InitiatedById { get; set; }
    public string? InitiatedByName { get; set; }
    public int? ConfirmedById { get; set; }
    public string? ConfirmedByName { get; set; }
    public int? CancelledById { get; set; }
    public string? CancelledByName { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsOwnRequest { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientBloodType { get; set; } = string.Empty;
    public RequestStatus RequestStatus { get; set; }
    public int CurrentStep { get; set; }
    public List<HandoverSessionItemDto> Items { get; set; } = new List<HandoverSessionItemDto>();
}

public class TransfusionScanDto
{
    public int AllocationId { get; set; }
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int ComponentTypeId { get; set; }
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime ExpiredAt { get; set; }
    public bool IsExpired { get; set; }
    public BagStatus BagStatus { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientBloodType { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? DoctorName { get; set; }
    public bool IsOwnRequest { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? ReceivedByName { get; set; }
}

public class ConfirmTransfusionRequest
{
    [Required(ErrorMessage = "Mã vạch túi máu (barcode) là bắt buộc.")]
    public string Barcode { get; set; } = string.Empty;

    public int? AllocationId { get; set; }

    [Required(ErrorMessage = "Mã người bệnh đối soát (expectedPatientCode) là bắt buộc.")]
    public string ExpectedPatientCode { get; set; } = string.Empty;
}

public class ConfirmTransfusionResponse
{
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public BagStatus BagStatus { get; set; }
    public int AllocationId { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientBloodType { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public int ConfirmedById { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime TransfusedAt { get; set; }
}

public class ReservedHandoverBagDto
{
    public int AllocationId { get; set; }
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int ComponentTypeId { get; set; }
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime ExpiredAt { get; set; }
    public bool IsExpired { get; set; }
    public string? LocationName { get; set; }
    public DateTime ReservedAt { get; set; }
    public string? ReservedByName { get; set; }
    public int? ActiveSessionId { get; set; }
    public string? ActiveSessionCode { get; set; }
}

public class ReservedHandoverRequestDto
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public RequestStatus RequestStatus { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientBloodType { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? DoctorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalReservedBags { get; set; }
    public int ReadyForSessionCount { get; set; }
    public int InPendingSessionCount { get; set; }
    public List<ReservedHandoverBagDto> Bags { get; set; } = new List<ReservedHandoverBagDto>();
}
