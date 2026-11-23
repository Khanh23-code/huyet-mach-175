using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Common;

namespace HuyetMach175.Modules.Clinical.Mappings;

public static class BloodReturnMappings
{
    public static CreateBloodReturnResponse ToCreateResponse(
        this BloodReturn bloodReturn,
        BloodAllocation allocation,
        User returnedByUser,
        bool isEligibleForRestockEvaluation,
        List<string> warningFlags)
    {
        var bag = allocation.Bag!;
        var request = allocation.Request!;

        return new CreateBloodReturnResponse
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = allocation.AllocationId,
            RequestId = request.RequestId,
            RequestCode = request.RequestCode,
            PatientCode = request.PatientCode,
            PatientName = request.PatientName,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            ReturnReason = bloodReturn.ReturnReason,
            Qualified = bloodReturn.Qualified,
            FinalAction = bloodReturn.FinalAction,
            BagStatus = bag.Status,
            ReturnedById = returnedByUser.UserId,
            ReturnedByName = returnedByUser.FullName,
            ProcessedAt = bloodReturn.ProcessedAt,
            IsEligibleForRestockEvaluation = isEligibleForRestockEvaluation,
            WarningFlags = warningFlags
        };
    }

    public static CancelBloodReturnResponse ToCancelResponse(
        this BloodReturn bloodReturn,
        BloodBag bag,
        User cancelledByUser,
        string? cancelReason)
    {
        return new CancelBloodReturnResponse
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = bloodReturn.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            FinalAction = bloodReturn.FinalAction,
            BagStatus = bag.Status,
            CancelReason = cancelReason,
            CancelledById = cancelledByUser.UserId,
            CancelledByName = cancelledByUser.FullName,
            CancelledAt = bloodReturn.ProcessedAt
        };
    }

    public static PendingBloodReturnDto ToPendingDto(
        this BloodReturn bloodReturn,
        int previousRestockCount,
        List<string> clinicalChecklistWarnings,
        DateTime nowUtc)
    {
        var allocation = bloodReturn.Allocation!;
        var bag = allocation.Bag!;
        var request = allocation.Request!;
        var componentType = bag.ComponentType;
        var mandatoryReasons = BloodReturnProcessing.EvaluateRestockEligibility(bag, previousRestockCount, nowUtc);

        return new PendingBloodReturnDto
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = bloodReturn.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentCode = componentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = componentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            VolumeMl = bag.VolumeMl,
            ExpiredAt = bag.ExpiredAt,
            IsExpired = bag.ExpiredAt <= nowUtc,
            RequestId = request.RequestId,
            RequestCode = request.RequestCode,
            PatientCode = request.PatientCode,
            PatientName = request.PatientName,
            DepartmentName = request.Doctor?.Department?.DepartmentName,
            ReturnedById = bloodReturn.ReturnedBy,
            ReturnedByName = bloodReturn.ReturnedByUser?.FullName,
            ReturnReason = bloodReturn.ReturnReason,
            FinalAction = bloodReturn.FinalAction,
            BagStatus = bag.Status,
            ProcessedAt = bloodReturn.ProcessedAt,
            AllowRestock = componentType?.AllowRestock ?? true,
            PreviousRestockCount = previousRestockCount,
            MaxRestockCount = componentType?.MaxRestockCount ?? 1,
            MaxOutsideMinutesForRestock = componentType?.MaxOutsideMinutesForRestock,
            MinTemp = componentType?.MinTemp ?? 0m,
            MaxTemp = componentType?.MaxTemp ?? 0m,
            StorageTemperatureLabel = BloodComponentStorageRules.StorageHint(componentType),
            RequiresMandatoryDiscard = mandatoryReasons.Count > 0,
            MandatoryDiscardReasons = mandatoryReasons,
            ClinicalChecklistWarnings = clinicalChecklistWarnings
        };
    }

    public static ReturnInspectionDto ToInspectionDto(
        this BloodReturn bloodReturn,
        int? previousLocationId,
        string? previousLocationName,
        int previousRestockCount,
        List<string> clinicalChecklistWarnings,
        DateTime nowUtc)
    {
        var allocation = bloodReturn.Allocation!;
        var bag = allocation.Bag!;
        var request = allocation.Request!;
        var componentType = bag.ComponentType;
        var mandatoryReasons = BloodReturnProcessing.EvaluateRestockEligibility(bag, previousRestockCount, nowUtc);
        double? minutesSinceIssued = allocation.IssuedAt.HasValue
            ? Math.Round(Math.Max(0, (bloodReturn.ProcessedAt - allocation.IssuedAt.Value).TotalMinutes), 1)
            : null;

        return new ReturnInspectionDto
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = bloodReturn.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentTypeId = bag.ComponentTypeId,
            ComponentCode = componentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = componentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            VolumeMl = bag.VolumeMl,
            CollectedAt = bag.CollectedAt,
            ExpiredAt = bag.ExpiredAt,
            IsExpired = bag.ExpiredAt <= nowUtc,
            BagStatus = bag.Status,
            PreviousLocationId = previousLocationId,
            PreviousLocationName = previousLocationName,
            RequestId = request.RequestId,
            RequestCode = request.RequestCode,
            PatientCode = request.PatientCode,
            PatientName = request.PatientName,
            DepartmentName = request.Doctor?.Department?.DepartmentName,
            DoctorName = request.Doctor?.FullName,
            ReturnedById = bloodReturn.ReturnedBy,
            ReturnedByName = bloodReturn.ReturnedByUser?.FullName,
            ReturnReason = bloodReturn.ReturnReason,
            Qualified = bloodReturn.Qualified,
            FinalAction = bloodReturn.FinalAction,
            ReturnedAt = bloodReturn.ProcessedAt,
            AllowRestock = componentType?.AllowRestock ?? true,
            PreviousRestockCount = previousRestockCount,
            MaxRestockCount = componentType?.MaxRestockCount ?? 1,
            MaxOutsideMinutesForRestock = componentType?.MaxOutsideMinutesForRestock,
            MinutesSinceIssued = minutesSinceIssued,
            MinTemp = componentType?.MinTemp ?? 0m,
            MaxTemp = componentType?.MaxTemp ?? 0m,
            StorageTemperatureLabel = BloodComponentStorageRules.StorageHint(componentType),
            RequiresMandatoryDiscard = mandatoryReasons.Count > 0,
            MandatoryDiscardReasons = mandatoryReasons,
            ClinicalChecklistWarnings = clinicalChecklistWarnings
        };
    }

    public static ProcessBloodReturnResponse ToProcessResponse(
        this BloodReturn bloodReturn,
        BloodBag bag,
        StorageLocation? targetLocation,
        string? appliedDiscardReason,
        User inspectedByUser,
        string message)
    {
        return new ProcessBloodReturnResponse
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = bloodReturn.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            Qualified = bloodReturn.Qualified,
            FinalAction = bloodReturn.FinalAction,
            BagStatus = bag.Status,
            AllocationStatus = bloodReturn.Allocation!.Status,
            NewLocationId = targetLocation?.LocationId,
            NewLocationName = targetLocation != null ? $"{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}" : null,
            DiscardReason = appliedDiscardReason,
            InspectedById = inspectedByUser.UserId,
            InspectedByName = inspectedByUser.FullName,
            ProcessedAt = bloodReturn.ProcessedAt,
            Message = message
        };
    }
}
