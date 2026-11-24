using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;
using HuyetMach175.Modules.Clinical.Processing;

namespace HuyetMach175.Modules.Clinical.Mappings;

public static class BloodHandoverMappings
{
    public static HandoverSessionDto ToDto(this BloodHandoverSession session, int currentUserId, DateTime nowUtc)
    {
        var remainingSeconds = session.Status == HandoverSessionStatus.PENDING
            ? Math.Max(0, (int)Math.Ceiling((session.ExpiresAt - nowUtc).TotalSeconds))
            : 0;

        return new HandoverSessionDto
        {
            HandoverSessionId = session.HandoverSessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt,
            RemainingSeconds = remainingSeconds,
            ConfirmedAt = session.ConfirmedAt,
            CancelledAt = session.CancelledAt,
            CancelReason = session.CancelReason,
            InitiatedById = session.InitiatedBy,
            InitiatedByName = session.InitiatedByUser?.FullName,
            ConfirmedById = session.ConfirmedBy,
            ConfirmedByName = session.ConfirmedByUser?.FullName,
            CancelledById = session.CancelledBy,
            CancelledByName = session.CancelledByUser?.FullName,
            RequestId = session.RequestId,
            RequestCode = session.Request?.RequestCode ?? string.Empty,
            DoctorId = session.Request?.DoctorId,
            DoctorName = session.Request?.Doctor?.FullName,
            DepartmentName = session.Request?.Doctor?.Department?.DepartmentName,
            IsOwnRequest = session.Request?.DoctorId == currentUserId,
            UrgencyLevel = session.Request?.UrgencyLevel ?? UrgencyLevel.ROUTINE,
            PatientCode = session.Request?.PatientCode ?? string.Empty,
            PatientName = session.Request?.PatientName ?? string.Empty,
            PatientBloodType = session.Request != null
                ? BloodCompatibilityRules.FormatBloodGroup(session.Request.PatientBloodType, session.Request.PatientRh)
                : string.Empty,
            RequestStatus = session.Request?.Status ?? RequestStatus.PENDING,
            CurrentStep = session.Request != null ? BloodRequestMappings.ResolveCurrentStep(session.Request.Status) : 0,
            Items = session.Items
                .OrderBy(i => i.ItemId)
                .Select(i => new HandoverSessionItemDto
                {
                    AllocationId = i.AllocationId,
                    BagId = i.BagId,
                    Barcode = i.Bag?.Barcode ?? string.Empty,
                    ComponentTypeId = i.Bag?.ComponentTypeId ?? 0,
                    ComponentCode = i.Bag?.ComponentType?.TypeCode ?? ComponentCode.WB,
                    ComponentName = i.Bag?.ComponentType?.TypeName ?? string.Empty,
                    BloodType = i.Bag != null
                        ? BloodCompatibilityRules.FormatBloodGroup(i.Bag.BloodType, i.Bag.RhFactor)
                        : string.Empty,
                    VolumeMl = i.Bag?.VolumeMl ?? 0,
                    ExpiredAt = i.Bag?.ExpiredAt ?? DateTime.MinValue,
                    BagStatus = i.Bag?.Status ?? BagStatus.RESERVED,
                    LocationName = i.Bag?.Location != null
                        ? $"{i.Bag.Location.RefrigeratorName} - {i.Bag.Location.ShelfNumber}"
                        : null
                })
                .ToList()
        };
    }

    public static TransfusionScanDto ToTransfusionScanDto(this BloodBag bag, BloodAllocation allocation, int currentUserId, DateTime nowUtc)
    {
        var request = allocation.Request!;
        return new TransfusionScanDto
        {
            AllocationId = allocation.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentTypeId = bag.ComponentTypeId,
            ComponentCode = bag.ComponentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            VolumeMl = bag.VolumeMl,
            ExpiredAt = bag.ExpiredAt,
            IsExpired = bag.ExpiredAt <= nowUtc,
            BagStatus = bag.Status,
            RequestId = request.RequestId,
            RequestCode = request.RequestCode,
            PatientName = request.PatientName,
            PatientCode = request.PatientCode,
            PatientBloodType = BloodCompatibilityRules.FormatBloodGroup(request.PatientBloodType, request.PatientRh),
            DepartmentName = request.Doctor?.Department?.DepartmentName,
            DoctorName = request.Doctor?.FullName,
            IsOwnRequest = request.DoctorId == currentUserId,
            IssuedAt = allocation.IssuedAt,
            ReceivedByName = allocation.ReceivedByUser?.FullName
        };
    }

    public static ConfirmTransfusionResponse ToConfirmTransfusionResponse(this BloodBag bag, BloodAllocation allocation, User currentUser, DateTime transfusedAt)
    {
        var request = allocation.Request!;
        return new ConfirmTransfusionResponse
        {
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            BagStatus = bag.Status,
            AllocationId = allocation.AllocationId,
            RequestId = request.RequestId,
            RequestCode = request.RequestCode,
            PatientCode = request.PatientCode,
            PatientName = request.PatientName,
            PatientBloodType = BloodCompatibilityRules.FormatBloodGroup(request.PatientBloodType, request.PatientRh),
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            ConfirmedById = currentUser.UserId,
            ConfirmedByName = currentUser.FullName,
            TransfusedAt = transfusedAt
        };
    }

    public static ReservedHandoverBagDto ToReservedHandoverBagDto(
        this BloodAllocation allocation,
        (int SessionId, string SessionCode)? activeSession,
        DateTime nowUtc)
    {
        var bag = allocation.Bag!;
        return new ReservedHandoverBagDto
        {
            AllocationId = allocation.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentTypeId = bag.ComponentTypeId,
            ComponentCode = bag.ComponentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            VolumeMl = bag.VolumeMl,
            ExpiredAt = bag.ExpiredAt,
            IsExpired = bag.ExpiredAt <= nowUtc,
            LocationName = bag.Location != null
                ? $"{bag.Location.RefrigeratorName} - {bag.Location.ShelfNumber}"
                : null,
            ReservedAt = allocation.ReservedAt,
            ReservedByName = allocation.ReservedByUser?.FullName,
            ActiveSessionId = activeSession?.SessionId,
            ActiveSessionCode = activeSession?.SessionCode
        };
    }
}
