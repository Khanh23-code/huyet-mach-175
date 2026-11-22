using System.ComponentModel.DataAnnotations;
using HuyetMach175.Api.Data.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;

public class PendingClinicalReceiptFilterDto
{
    [FromQuery(Name = "doctorId")]
    public int? DoctorId { get; set; }

    [FromQuery(Name = "doctorName")]
    public string? DoctorName { get; set; }

    [FromQuery(Name = "requestCode")]
    public string? RequestCode { get; set; }
}

public class DispatchBloodBagRequest
{
    [Required(ErrorMessage = "Mã vạch túi máu (barcode) là bắt buộc.")]
    public string Barcode { get; set; } = string.Empty;
}

public class DispatchBloodBagResponse
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
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientBloodType { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? DoctorName { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public RequestStatus RequestStatus { get; set; }
    public int IssuedById { get; set; }
    public string? IssuedByName { get; set; }
    public DateTime IssuedAt { get; set; }
}

public class PendingReceiptBagDto
{
    public int AllocationId { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsOwnRequest { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? IssuedByName { get; set; }
}

public class ConfirmReceiptRequest
{
    [Required(ErrorMessage = "Mã phân bổ (allocationId) là bắt buộc.")]
    public int AllocationId { get; set; }

    [Required(ErrorMessage = "Mã vạch quét được (scannedBarcode) là bắt buộc.")]
    public string ScannedBarcode { get; set; } = string.Empty;

    public string? ExpectedPatientCode { get; set; }
}

public class ConfirmReceiptResponse
{
    public int AllocationId { get; set; }
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int BagId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public ComponentCode ComponentCode { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public AllocationStatus AllocationStatus { get; set; }
    public RequestStatus RequestStatus { get; set; }
    public int CurrentStep { get; set; }
    public int ReceivedById { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime ConfirmedAt { get; set; }
}
