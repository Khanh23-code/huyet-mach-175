using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.Processing;

namespace HuyetMach175.Modules.Inventory.Mappings;

public static class BloodDisposalMappings
{
    public static DisposalRecordDto ToRecordDto(this BloodDisposalRecord r)
    {
        var barcodes = r.Items
            .Select(i => i.BloodBag?.Barcode)
            .Where(b => !string.IsNullOrEmpty(b))
            .ToList();

        var barcodesSummary = barcodes.Count switch
        {
            0 => string.Empty,
            1 => barcodes[0]!,
            2 => $"{barcodes[0]}, {barcodes[1]}",
            _ => $"{barcodes[0]}, {barcodes[1]} (+{barcodes.Count - 2} túi)"
        };

        return new DisposalRecordDto
        {
            DisposalId = r.DisposalId,
            DisposalCode = r.DisposalCode,
            TotalBags = r.TotalBags > 0 ? r.TotalBags : r.Items.Count,
            BarcodesSummary = barcodesSummary,
            Reason = r.Reason.ToString(),
            ReasonText = FormatDisposalReasonText(r.Reason),
            Notes = r.Notes,
            Status = r.Status.ToString(),
            StatusText = FormatDisposalStatusText(r.Status),
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
        var dto = new DisposalDetailDto
        {
            DisposalId = r.DisposalId,
            DisposalCode = r.DisposalCode,
            TotalBags = r.TotalBags > 0 ? r.TotalBags : r.Items.Count,
            BarcodesSummary = string.Join(", ", r.Items.Select(i => i.BloodBag?.Barcode).Where(b => !string.IsNullOrEmpty(b))),
            Reason = r.Reason.ToString(),
            ReasonText = FormatDisposalReasonText(r.Reason),
            Notes = r.Notes,
            Status = r.Status.ToString(),
            StatusText = FormatDisposalStatusText(r.Status),
            RequestedBy = r.RequestedBy,
            RequestedByName = r.RequestedByUser?.FullName ?? string.Empty,
            RequestedAt = r.RequestedAt,
            ApprovedBy = r.ApprovedBy,
            ApprovedByName = r.ApprovedByUser?.FullName,
            ApprovedAt = r.ApprovedAt,
            RejectionReason = r.RejectionReason,
            Items = r.Items.Select(ToItemDto).ToList()
        };

        return dto;
    }

    public static DisposalItemDto ToItemDto(this BloodDisposalItem i)
    {
        var bag = i.BloodBag;
        return new DisposalItemDto
        {
            ItemId = i.ItemId,
            BagId = i.BagId,
            Barcode = bag?.Barcode ?? string.Empty,
            BloodType = bag?.BloodType.ToString() ?? string.Empty,
            RhFactor = bag?.RhFactor.ToString() ?? string.Empty,
            BloodGroup = bag != null ? InventoryProcessing.FormatBloodGroup(bag.BloodType, bag.RhFactor) : string.Empty,
            ComponentTypeCode = bag?.ComponentType?.TypeCode.ToString() ?? string.Empty,
            ComponentTypeName = bag?.ComponentType?.TypeName ?? string.Empty,
            VolumeMl = bag?.VolumeMl ?? 0,
            StorageLocationId = bag?.LocationId,
            StorageLocationName = bag?.Location != null ? $"{bag.Location.RefrigeratorName} - {bag.Location.ShelfNumber}" : null,
            CollectedAt = bag?.CollectedAt,
            ExpiredAt = bag?.ExpiredAt,
            PreviousStatus = i.PreviousStatus.ToString(),
            PreviousStatusText = FormatBagStatusText(i.PreviousStatus),
            CurrentBagStatus = bag != null ? bag.Status.ToString() : string.Empty,
            CurrentBagStatusText = bag != null ? FormatBagStatusText(bag.Status) : string.Empty,
            IsVerified = i.IsVerified,
            VerifiedAt = i.VerifiedAt
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
        DisposalStatus.APPROVED => "Đã phê duyệt (Chờ tiêu hủy)",
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