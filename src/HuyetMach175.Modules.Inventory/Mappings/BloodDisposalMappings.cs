using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.Processing;

namespace HuyetMach175.Modules.Inventory.Mappings;

public static class BloodDisposalMappings
{
    public static DisposalRecordDto ToRecordDto(this BloodDisposalRecord r)
    {
        return new DisposalRecordDto
        {
            DisposalId = r.DisposalId,
            DisposalCode = r.DisposalCode,
            BagId = r.BagId,
            Barcode = r.BloodBag?.Barcode ?? string.Empty,
            BloodType = r.BloodBag?.BloodType.ToString() ?? string.Empty,
            RhFactor = r.BloodBag?.RhFactor.ToString() ?? string.Empty,
            BloodGroup = r.BloodBag != null ? InventoryProcessing.FormatBloodGroup(r.BloodBag.BloodType, r.BloodBag.RhFactor) : string.Empty,
            ComponentTypeCode = r.BloodBag?.ComponentType?.TypeCode.ToString() ?? string.Empty,
            ComponentTypeName = r.BloodBag?.ComponentType?.TypeName ?? string.Empty,
            VolumeMl = r.BloodBag?.VolumeMl ?? 0,
            Reason = r.Reason.ToString(),
            ReasonText = FormatDisposalReasonText(r.Reason),
            Notes = r.Notes,
            Status = r.Status.ToString(),
            StatusText = FormatDisposalStatusText(r.Status),
            PreviousStatus = r.PreviousStatus.ToString(),
            RequestedBy = r.RequestedBy,
            RequestedByName = r.RequestedByUser?.FullName ?? string.Empty,
            RequestedAt = r.RequestedAt,
            ApprovedBy = r.ApprovedBy,
            ApprovedByName = r.ApprovedByUser?.FullName,
            ApprovedAt = r.ApprovedAt,
            RejectionReason = r.RejectionReason
        };
    }

    public static DisposalDetailDto ToDetailDto(this BloodDisposalRecord r)
    {
        return new DisposalDetailDto
        {
            DisposalId = r.DisposalId,
            DisposalCode = r.DisposalCode,
            BagId = r.BagId,
            Barcode = r.BloodBag?.Barcode ?? string.Empty,
            BloodType = r.BloodBag?.BloodType.ToString() ?? string.Empty,
            RhFactor = r.BloodBag?.RhFactor.ToString() ?? string.Empty,
            BloodGroup = r.BloodBag != null ? InventoryProcessing.FormatBloodGroup(r.BloodBag.BloodType, r.BloodBag.RhFactor) : string.Empty,
            ComponentTypeCode = r.BloodBag?.ComponentType?.TypeCode.ToString() ?? string.Empty,
            ComponentTypeName = r.BloodBag?.ComponentType?.TypeName ?? string.Empty,
            VolumeMl = r.BloodBag?.VolumeMl ?? 0,
            Reason = r.Reason.ToString(),
            ReasonText = FormatDisposalReasonText(r.Reason),
            Notes = r.Notes,
            Status = r.Status.ToString(),
            StatusText = FormatDisposalStatusText(r.Status),
            PreviousStatus = r.PreviousStatus.ToString(),
            RequestedBy = r.RequestedBy,
            RequestedByName = r.RequestedByUser?.FullName ?? string.Empty,
            RequestedAt = r.RequestedAt,
            ApprovedBy = r.ApprovedBy,
            ApprovedByName = r.ApprovedByUser?.FullName,
            ApprovedAt = r.ApprovedAt,
            RejectionReason = r.RejectionReason,
            StorageLocationId = r.BloodBag?.LocationId,
            StorageLocationName = r.BloodBag?.Location != null ? $"{r.BloodBag.Location.RefrigeratorName} - {r.BloodBag.Location.ShelfNumber}" : null,
            CollectedAt = r.BloodBag?.CollectedAt,
            ExpiredAt = r.BloodBag?.ExpiredAt,
            CurrentBagStatus = r.BloodBag != null ? r.BloodBag.Status.ToString() : string.Empty,
            CurrentBagStatusText = r.BloodBag != null ? FormatBagStatusText(r.BloodBag.Status) : string.Empty,
            DiscardReason = r.BloodBag?.DiscardReason
        };
    }

    public static string FormatDisposalReasonText(DisposalReason reason) => reason switch
    {
        DisposalReason.EXPIRED => "Hết hạn sử dụng",
        DisposalReason.INFECTIOUS_POSITIVE => "Dương tính bệnh truyền nhiễm",
        DisposalReason.HEMOLYSIS => "Máu tán huyết / biến chất",
        DisposalReason.DAMAGED_BAG => "Hỏng túi / rò rỉ",
        DisposalReason.OTHER => "Lý do khác",
        _ => reason.ToString()
    };

    public static string FormatDisposalStatusText(DisposalStatus status) => status switch
    {
        DisposalStatus.PENDING_APPROVAL => "Chờ phê duyệt",
        DisposalStatus.DISPOSED => "Đã tiêu hủy",
        DisposalStatus.REJECTED => "Từ chối tiêu hủy",
        DisposalStatus.CANCELLED => "Đã hủy yêu cầu",
        _ => status.ToString()
    };

    public static string FormatBagStatusText(BagStatus status) => status switch
    {
        BagStatus.NEWLY_COLLECTED => "Mới thu gom",
        BagStatus.READY_TESTING => "Chờ xét nghiệm",
        BagStatus.TESTED_PASSED => "Đã xét nghiệm đạt",
        BagStatus.AVAILABLE => "Sẵn sàng cấp phát",
        BagStatus.LOCKED => "Tạm khóa (Chờ tiêu hủy)",
        BagStatus.RESERVED => "Đã dự trù",
        BagStatus.ISSUED => "Đã xuất viện",
        BagStatus.TRANSFUSED => "Đã truyền máu",
        BagStatus.DISCARDED => "Đã tiêu hủy",
        BagStatus.PROCESSED => "Đã điều chế",
        _ => status.ToString()
    };
}