using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class BloodRequestDetailDto
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public BloodType PatientBloodType { get; set; }
    public RhFactor PatientRh { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public string? Diagnosis { get; set; }
    public RequestStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public string? RejectionReason { get; set; }
    public int TotalRequestedUnits { get; set; }
    public int TotalAllocatedUnits { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BloodRequestItemDetailDto> Items { get; set; } = new List<BloodRequestItemDetailDto>();
    public List<BloodRequestAllocationDto> Allocations { get; set; } = new List<BloodRequestAllocationDto>();
}
