using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;

namespace HuyetMach175.Modules.Clinical.Mappings;

public static class BloodRequestMappings
{
    public static int ResolveCurrentStep(RequestStatus status) => status switch
    {
        RequestStatus.DRAFT => 0,
        RequestStatus.PENDING => 1,
        RequestStatus.READY => 2,
        RequestStatus.COMPLETED => 3,
        RequestStatus.REJECTED => -1,
        _ => 0
    };

    public static ClinicalBloodRequestDto ToClinicalDto(this BloodRequest request)
    {
        return new ClinicalBloodRequestDto
        {
            RequestId = request.RequestId,
            RequestCode = request.RequestCode,
            PatientCode = request.PatientCode,
            PatientName = request.PatientName,
            PatientBloodType = request.PatientBloodType,
            PatientRh = request.PatientRh,
            TotalRequestedUnits = request.Items.Sum(i => i.RequestedUnits),
            TotalAllocatedUnits = request.Items.Sum(i => i.AllocatedUnits),
            UrgencyLevel = request.UrgencyLevel,
            Status = request.Status,
            CurrentStep = ResolveCurrentStep(request.Status),
            Diagnosis = request.Diagnosis,
            CreatedAt = request.CreatedAt
        };
    }

    public static BloodRequestDetailDto ToDetailDto(this BloodRequest request)
    {
        return new BloodRequestDetailDto
        {
            RequestId = request.RequestId,
            RequestCode = request.RequestCode,
            DoctorId = request.DoctorId,
            DoctorName = request.Doctor?.FullName,
            DepartmentId = request.Doctor?.DepartmentId,
            DepartmentName = request.Doctor?.Department?.DepartmentName,
            PatientCode = request.PatientCode,
            PatientName = request.PatientName,
            PatientBloodType = request.PatientBloodType,
            PatientRh = request.PatientRh,
            UrgencyLevel = request.UrgencyLevel,
            Diagnosis = request.Diagnosis,
            Status = request.Status,
            CurrentStep = ResolveCurrentStep(request.Status),
            RejectionReason = request.RejectionReason,
            TotalRequestedUnits = request.Items.Sum(i => i.RequestedUnits),
            TotalAllocatedUnits = request.Items.Sum(i => i.AllocatedUnits),
            CreatedAt = request.CreatedAt,
            Items = request.Items
                .OrderBy(i => i.RequestItemId)
                .Select(i => new BloodRequestItemDetailDto
                {
                    RequestItemId = i.RequestItemId,
                    ComponentTypeId = i.ComponentTypeId,
                    ComponentCode = i.ComponentType?.TypeCode ?? ComponentCode.WB,
                    ComponentName = i.ComponentType?.TypeName ?? string.Empty,
                    RequestedUnits = i.RequestedUnits,
                    AllocatedUnits = i.AllocatedUnits
                })
                .ToList(),
            Allocations = request.Allocations
                .OrderBy(a => a.AllocationId)
                .Select(a => new BloodRequestAllocationDto
                {
                    AllocationId = a.AllocationId,
                    BagId = a.BagId,
                    Barcode = a.Bag?.Barcode ?? string.Empty,
                    ComponentTypeId = a.Bag?.ComponentTypeId ?? 0,
                    ComponentCode = a.Bag?.ComponentType?.TypeCode,
                    ComponentName = a.Bag?.ComponentType?.TypeName ?? string.Empty,
                    BloodType = a.Bag?.BloodType,
                    RhFactor = a.Bag?.RhFactor,
                    VolumeMl = a.Bag?.VolumeMl ?? 0,
                    ExpiredAt = a.Bag?.ExpiredAt,
                    BagStatus = a.Bag?.Status,
                    Status = a.Status,
                    ReservedAt = a.ReservedAt,
                    ReservedByName = a.ReservedByUser?.FullName,
                    IssuedAt = a.IssuedAt,
                    IssuedByName = a.IssuedByUser?.FullName,
                    ReceivedByName = a.ReceivedByUser?.FullName
                })
                .ToList()
        };
    }
}
