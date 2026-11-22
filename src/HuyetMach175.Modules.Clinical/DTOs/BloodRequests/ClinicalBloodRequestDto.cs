using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

public class ClinicalBloodRequestDto
{
    public int RequestId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public BloodType PatientBloodType { get; set; }
    public RhFactor PatientRh { get; set; }
    public int TotalRequestedUnits { get; set; }
    public int TotalAllocatedUnits { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public RequestStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public string? Diagnosis { get; set; }
    public DateTime CreatedAt { get; set; }
}
