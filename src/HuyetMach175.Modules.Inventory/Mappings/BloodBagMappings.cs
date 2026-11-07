using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodBags;

namespace HuyetMach175.Modules.Inventory.Mappings;

public static class BloodBagMappings
{
    public static string FormatBloodGroup(BloodType bloodType, RhFactor rhFactor)
    {
        var typeStr = bloodType == BloodType.AB ? "AB" : bloodType.ToString();
        var rhStr = rhFactor == RhFactor.POSITIVE ? "+" : "-";
        return $"{typeStr}{rhStr}";
    }

    public static string FormatLifecycleStatusLabel(BagStatus status) => status switch
    {
        BagStatus.NEWLY_COLLECTED => "Tiếp nhận",
        BagStatus.READY_TESTING => "Cách ly",
        BagStatus.TESTED_PASSED => "Xét nghiệm",
        BagStatus.AVAILABLE => "Nhập kho",
        BagStatus.RESERVED => "Giữ chỗ",
        BagStatus.ISSUED => "Cấp phát",
        BagStatus.TRANSFUSED => "Đã truyền",
        BagStatus.LOCKED => "Tạm khóa",
        BagStatus.DISCARDED => "Đã tiêu hủy",
        BagStatus.PROCESSED => "Đã điều chế",
        _ => status.ToString()
    };

    public static int GetLifecycleStepIndex(BagStatus status) => status switch
    {
        BagStatus.NEWLY_COLLECTED => 1,
        BagStatus.READY_TESTING => 2,
        BagStatus.TESTED_PASSED => 3,
        BagStatus.AVAILABLE => 4,
        BagStatus.RESERVED => 5,
        BagStatus.ISSUED => 6,
        BagStatus.TRANSFUSED => 7,
        _ => 0
    };

    public static (BagStatus? NextStatus, string? NextStatusLabel, int? NextStep) GetNextStep(BagStatus current) => current switch
    {
        BagStatus.NEWLY_COLLECTED => (BagStatus.READY_TESTING, "Cách ly", 2),
        BagStatus.READY_TESTING => (BagStatus.TESTED_PASSED, "Xét nghiệm", 3),
        BagStatus.TESTED_PASSED => (BagStatus.AVAILABLE, "Nhập kho", 4),
        BagStatus.AVAILABLE => (BagStatus.RESERVED, "Giữ chỗ", 5),
        BagStatus.RESERVED => (BagStatus.ISSUED, "Cấp phát", 6),
        BagStatus.ISSUED => (BagStatus.TRANSFUSED, "Đã truyền", 7),
        _ => (null, null, null)
    };

    public static BloodBagDto ToDto(this BloodBag b) => new()
    {
        BagId = b.BagId,
        Barcode = b.Barcode,
        ParentBagId = b.ParentBagId,
        ParentBarcode = b.ParentBag?.Barcode,
        ComponentTypeId = b.ComponentTypeId,
        ComponentTypeCode = b.ComponentType?.TypeCode.ToString() ?? string.Empty,
        ComponentTypeName = b.ComponentType?.TypeName ?? string.Empty,
        BloodType = b.BloodType.ToString(),
        RhFactor = b.RhFactor.ToString(),
        BloodGroup = FormatBloodGroup(b.BloodType, b.RhFactor),
        VolumeMl = b.VolumeMl,
        Status = b.Status.ToString(),
        StorageLocationId = b.LocationId,
        StorageLocationName = b.Location != null ? $"{b.Location.RefrigeratorName} - {b.Location.ShelfNumber}" : null,
        CollectedAt = b.CollectedAt,
        ExpiredAt = b.ExpiredAt,
        CreatedAt = b.CreatedAt
    };

    public static ReceiveBloodBagResponse ToReceiveResponse(
        this BloodBag bloodBag,
        StorageLocation location,
        decimal receivedTemperature,
        DateTime receivedAt,
        int receivedBy) => new()
    {
        BagId = bloodBag.BagId,
        Barcode = bloodBag.Barcode,
        ComponentType = bloodBag.ComponentType?.TypeName ?? bloodBag.ComponentType?.TypeCode.ToString() ?? "Máu toàn phần",
        BloodType = bloodBag.BloodType.ToString(),
        RhFactor = bloodBag.RhFactor.ToString(),
        BloodGroup = FormatBloodGroup(bloodBag.BloodType, bloodBag.RhFactor),
        VolumeMl = bloodBag.VolumeMl,
        Status = bloodBag.Status.ToString(),
        StorageLocationId = location.LocationId,
        StorageLocationName = $"{location.RefrigeratorName} - {location.ShelfNumber}",
        ReceivedTemperature = receivedTemperature,
        CollectedAt = bloodBag.CollectedAt,
        ExpiredAt = bloodBag.ExpiredAt,
        ReceivedAt = receivedAt,
        ReceivedBy = receivedBy,
        Message = $"Tiếp nhận túi máu {bloodBag.Barcode} thành công. Trạng thái chuyển sang READY_TESTING sẵn sàng xét nghiệm."
    };

    public static BloodBagScanResponse ToScanResponse(this BloodBag bag, List<BloodBagStatusLog> logs)
    {
        var now = DateTime.UtcNow;
        var isExpired = bag.ExpiredAt <= now;
        var isLocked = bag.Status == BagStatus.LOCKED;
        var isDiscarded = bag.Status == BagStatus.DISCARDED;

        var currentStep = GetLifecycleStepIndex(bag.Status);
        if ((isLocked || isDiscarded) && currentStep == 0)
        {
            var lastNormal = logs.LastOrDefault(l => l.ToStatus != BagStatus.LOCKED && l.ToStatus != BagStatus.DISCARDED);
            currentStep = lastNormal != null ? GetLifecycleStepIndex(lastNormal.ToStatus) : 1;
        }

        var (nextStatus, nextStatusLabel, nextStep) = (isLocked || isDiscarded)
            ? (null, null, null)
            : GetNextStep(bag.Status);

        var stepsConfig = new (int Step, BagStatus Status, string Name)[]
        {
            (1, BagStatus.NEWLY_COLLECTED, "Tiếp nhận"),
            (2, BagStatus.READY_TESTING, "Cách ly"),
            (3, BagStatus.TESTED_PASSED, "Xét nghiệm"),
            (4, BagStatus.AVAILABLE, "Nhập kho"),
            (5, BagStatus.RESERVED, "Giữ chỗ"),
            (6, BagStatus.ISSUED, "Cấp phát"),
            (7, BagStatus.TRANSFUSED, "Đã truyền")
        };

        var timeline = new List<BloodBagTimelineStepDto>(7);
        foreach (var sc in stepsConfig)
        {
            var log = logs.LastOrDefault(l => l.ToStatus == sc.Status);
            bool completed = false;
            DateTime? timestamp = null;

            if (log != null)
            {
                completed = true;
                timestamp = log.CreatedAt;
            }
            else if (sc.Step <= currentStep)
            {
                completed = true;
                if (sc.Step == 1)
                {
                    timestamp = bag.CollectedAt;
                }
            }

            timeline.Add(new BloodBagTimelineStepDto
            {
                Step = sc.Step,
                StatusCode = sc.Status.ToString(),
                Name = sc.Name,
                Completed = completed,
                Timestamp = timestamp
            });
        }

        return new BloodBagScanResponse
        {
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            BloodType = bag.BloodType.ToString(),
            RhFactor = bag.RhFactor.ToString(),
            BloodGroup = FormatBloodGroup(bag.BloodType, bag.RhFactor),
            ComponentType = bag.ComponentType?.TypeName ?? "Chưa xác định",
            ComponentTypeCode = bag.ComponentType?.TypeCode.ToString() ?? string.Empty,
            VolumeMl = bag.VolumeMl,
            CurrentStatus = bag.Status.ToString(),
            CurrentStatusLabel = FormatLifecycleStatusLabel(bag.Status),
            CurrentStep = currentStep,
            NextStatus = nextStatus?.ToString(),
            NextStatusLabel = nextStatusLabel,
            NextStep = nextStep,
            IsLocked = isLocked,
            IsDiscarded = isDiscarded,
            IsExpired = isExpired,
            ExpiredAt = bag.ExpiredAt,
            CollectedAt = bag.CollectedAt,
            StorageLocation = bag.Location != null
                ? new StorageLocationSummaryDto
                {
                    LocationId = bag.Location.LocationId,
                    LocationName = $"{bag.Location.RefrigeratorName} - {bag.Location.ShelfNumber}"
                }
                : null,
            Timeline = timeline
        };
    }

    public static RecentStatusLogDto ToRecentLogDto(this BloodBagStatusLog l) => new()
    {
        LogId = l.LogId,
        Barcode = l.BloodBag?.Barcode ?? string.Empty,
        BloodGroup = l.BloodBag != null
            ? FormatBloodGroup(l.BloodBag.BloodType, l.BloodBag.RhFactor)
            : string.Empty,
        PreviousStatus = l.FromStatus.ToString(),
        PreviousStatusName = FormatLifecycleStatusLabel(l.FromStatus),
        NewStatus = l.ToStatus.ToString(),
        NewStatusName = FormatLifecycleStatusLabel(l.ToStatus),
        PerformedBy = l.PerformedByUser?.FullName ?? "Hệ thống (Tự động)",
        Notes = l.Notes,
        Timestamp = l.CreatedAt
    };
}