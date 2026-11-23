using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.SharedKernel.Common;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Processing;

public static class BloodReturnProcessing
{
    public static (string ReturnReason, List<string> FailedChecklistFlags) ValidateCreateReturnInput(CreateBloodReturnRequest request)
    {
        if (request.AllocationId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phân bổ (allocationId) không hợp lệ.");
        }

        var returnReason = request.ReturnReason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(returnReason))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do hoàn trả (returnReason) không được để trống.");
        }

        if (returnReason.Length > 255)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do hoàn trả không được vượt quá 255 ký tự.");
        }

        if (request.Checklist == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Bảng kiểm điều kiện hoàn trả (checklist) không được để trống.");
        }

        return (returnReason, GetFailedCreateChecklistCriteria(request.Checklist));
    }

    public static List<string> EvaluateRestockEligibility(BloodBag bag, int previousRestockCount, DateTime nowUtc)
    {
        var reasons = new List<string>();
        var componentType = bag.ComponentType;

        if (bag.ExpiredAt <= nowUtc)
        {
            reasons.Add($"Túi máu đã hết hạn sử dụng vào {bag.ExpiredAt:dd/MM/yyyy HH:mm}.");
        }

        if (componentType != null && !componentType.AllowRestock)
        {
            reasons.Add($"Chế phẩm {componentType.TypeName} ({componentType.TypeCode}) không được phép tái nhập kho theo cấu hình.");
        }

        var maxRestockCount = componentType?.MaxRestockCount ?? 1;
        if (previousRestockCount >= maxRestockCount)
        {
            reasons.Add($"Túi máu đã tái nhập kho {previousRestockCount}/{maxRestockCount} lần, vượt giới hạn cho phép.");
        }

        return reasons;
    }

    public static async Task<int> CountPreviousRestocksAsync(ApplicationDbContext context, int bagId)
    {
        return await context.BloodReturns
            .AsNoTracking()
            .CountAsync(br =>
                br.Allocation != null &&
                br.Allocation.BagId == bagId &&
                br.FinalAction == ReturnFinalAction.RESTOCK);
    }

    public static async Task<Dictionary<int, int>> CountPreviousRestocksByBagIdsAsync(ApplicationDbContext context, IEnumerable<int> bagIds)
    {
        var idList = bagIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await context.BloodReturns
            .AsNoTracking()
            .Where(br =>
                br.Allocation != null &&
                idList.Contains(br.Allocation.BagId) &&
                br.FinalAction == ReturnFinalAction.RESTOCK)
            .GroupBy(br => br.Allocation!.BagId)
            .Select(g => new { BagId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BagId, x => x.Count);
    }

    public static async Task<Dictionary<int, List<string>>> GetClinicalChecklistWarningsAsync(
        ApplicationDbContext context,
        IEnumerable<int> returnIds)
    {
        var idList = returnIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<int, List<string>>();
        }

        var logs = await context.AuditLogs
            .AsNoTracking()
            .Where(a =>
                a.TableName == "blood_returns" &&
                a.ActionType == ActionType.INSERT &&
                idList.Contains(a.RecordId) &&
                a.NewState != null)
            .OrderByDescending(a => a.LogId)
            .Select(a => new { a.RecordId, a.NewState })
            .ToListAsync();

        var result = new Dictionary<int, List<string>>();
        foreach (var log in logs)
        {
            if (result.ContainsKey(log.RecordId))
            {
                continue;
            }

            result[log.RecordId] = ExtractFailedChecklistFlags(log.NewState);
        }

        return result;
    }

    public static void ValidateAllocationForReturn(BloodAllocation? allocation, int allocationId, User currentUser)
    {
        if (allocation == null || allocation.Bag == null || allocation.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lượt phân bổ túi máu #{allocationId}.");
        }

        BloodRequestProcessing.EnsureSameDepartmentAccess(
            currentUser,
            allocation.Request,
            "Bạn chỉ có quyền lập phiếu hoàn trả cho túi máu thuộc khoa của mình.");

        var activeReturn = allocation.BloodReturns
            .FirstOrDefault(r => r.FinalAction != ReturnFinalAction.CANCELLED);

        if (activeReturn != null)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Lượt phân bổ #{allocation.AllocationId} (túi {allocation.Bag.Barcode}) đã được lập phiếu hoàn trả trước đó (#{activeReturn.ReturnId}).");
        }

        if (allocation.Bag.Status == BagStatus.TRANSFUSED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {allocation.Bag.Barcode} đã được xác nhận truyền cho người bệnh (TRANSFUSED), không thể lập phiếu hoàn trả.");
        }

        if (allocation.Bag.Status != BagStatus.ISSUED ||
            (allocation.Status != AllocationStatus.ISSUED && allocation.Status != AllocationStatus.COMPLETED))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {allocation.Bag.Barcode} đang ở trạng thái '{allocation.Bag.Status}' (phân bổ '{allocation.Status}'), chỉ có thể lập phiếu hoàn trả cho túi đã xuất kho.");
        }
    }

    public static void ValidatePendingReturnForInspection(BloodReturn? bloodReturn, int returnId, string notLockedMessage)
    {
        if (bloodReturn == null || bloodReturn.Allocation == null || bloodReturn.Allocation.Bag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu hoàn trả #{returnId}.");
        }

        if (bloodReturn.FinalAction == ReturnFinalAction.CANCELLED)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiếu hoàn trả #{returnId} đã được hủy trước đó.");
        }

        if (bloodReturn.FinalAction != ReturnFinalAction.PENDING_INSPECTION)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiếu hoàn trả #{returnId} đã được xử lý trước đó với kết quả '{bloodReturn.FinalAction}'.");
        }

        var bag = bloodReturn.Allocation.Bag;
        if (bag.Status != BagStatus.LOCKED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, notLockedMessage);
        }
    }

    public static async Task<StorageLocation> ValidateRestockTargetLocationAsync(
        ApplicationDbContext context,
        BloodBag bag,
        int previousRestockCount,
        BloodReturnInspectionChecklistDto? checklist,
        int? storageLocationId,
        DateTime nowUtc)
    {
        var componentType = bag.ComponentType;

        if (bag.ExpiredAt <= nowUtc)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đã hết hạn sử dụng vào {bag.ExpiredAt:dd/MM/yyyy HH:mm}, không thể tái nhập kho. Vui lòng chọn 'DISCARD'.");
        }

        if (componentType != null && !componentType.AllowRestock)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Chế phẩm {componentType.TypeName} ({componentType.TypeCode}) không được phép tái nhập kho theo cấu hình nghiệp vụ. Vui lòng chọn 'DISCARD'.");
        }

        var maxRestockCount = componentType?.MaxRestockCount ?? 1;
        if (previousRestockCount >= maxRestockCount)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Túi máu {bag.Barcode} đã tái nhập kho {previousRestockCount}/{maxRestockCount} lần, vượt giới hạn cho phép tái nhập kho. Vui lòng chọn 'DISCARD'.");
        }

        if (checklist == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Bảng kiểm 5 điều kiện bảo quản (checklist) là bắt buộc khi nhập lại kho.");
        }

        var failedCriteria = GetFailedInspectionCriteria(checklist, componentType);
        if (failedCriteria.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu không đạt đủ 5 điều kiện tái nhập kho: {string.Join("; ", failedCriteria)}.");
        }

        if (!storageLocationId.HasValue || storageLocationId.Value <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vị trí ngăn tủ lưu trữ mới (newStorageLocationId) là bắt buộc khi nhập lại kho.");
        }

        var targetLocation = await context.StorageLocations
            .FirstOrDefaultAsync(l => l.LocationId == storageLocationId.Value);

        if (targetLocation == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với ID {storageLocationId.Value}.");
        }

        var targetLocationName = $"{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}";

        if (!targetLocation.IsActive)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{targetLocationName}' đang ngừng hoạt động.");
        }

        if (targetLocation.IsFull)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{targetLocationName}' hiện đã đầy.");
        }

        if (targetLocation.Zone != StorageZone.RELEASED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Vị trí tái nhập kho '{targetLocationName}' thuộc phân khu '{targetLocation.Zone}', bắt buộc phải thuộc phân khu sẵn sàng cấp phát (RELEASED).");
        }

        if (!BloodComponentStorageRules.IsStorageSuitable(componentType, targetLocation.TargetTemperature))
        {
            var componentLabel = componentType?.TypeName ?? componentType?.TypeCode.ToString() ?? "chế phẩm máu";
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Vị trí '{targetLocationName}' ({targetLocation.TargetTemperature:0.#}°C) không phù hợp để bảo quản {componentLabel} (cần {BloodComponentStorageRules.StorageHint(componentType)}).");
        }

        return targetLocation;
    }

    public static async Task<StorageLocation?> ResolveDiscardHoldLocationAsync(ApplicationDbContext context, int? requestedLocationId)
    {
        if (requestedLocationId.HasValue)
        {
            if (requestedLocationId.Value <= 0)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Mã vị trí lưu trữ chờ hủy (newStorageLocationId) không hợp lệ.");
            }

            var requestedLocation = await context.StorageLocations
                .FirstOrDefaultAsync(l => l.LocationId == requestedLocationId.Value);

            if (requestedLocation == null)
            {
                throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với ID {requestedLocationId.Value}.");
            }

            var requestedName = $"{requestedLocation.RefrigeratorName} - {requestedLocation.ShelfNumber}";

            if (!requestedLocation.IsActive)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{requestedName}' đang ngừng hoạt động.");
            }

            if (requestedLocation.IsFull)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{requestedName}' hiện đã đầy.");
            }

            if (requestedLocation.Zone != StorageZone.DISPOSAL_HOLD)
            {
                throw new HttpException(StatusCodes.Status400BadRequest,
                    $"Vị trí '{requestedName}' thuộc phân khu '{requestedLocation.Zone}'. Túi máu không đạt hoàn trả bắt buộc đưa vào phân khu cách ly chờ hủy (DISPOSAL_HOLD).");
            }

            return requestedLocation;
        }

        return await context.StorageLocations
            .Where(l => l.IsActive && !l.IsFull && l.Zone == StorageZone.DISPOSAL_HOLD)
            .OrderBy(l => l.LocationId)
            .FirstOrDefaultAsync();
    }

    public static async Task<(int? LocationId, string? LocationName)> ResolvePreviousLocationAsync(
        ApplicationDbContext context,
        int allocationId,
        int bagId)
    {
        var previousLocationId = await FindLocationIdInAllocationLogsAsync(context, allocationId)
            ?? await FindLocationIdInHandoverSessionLogsAsync(context, allocationId, bagId);

        if (!previousLocationId.HasValue)
        {
            return (null, null);
        }

        var location = await context.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LocationId == previousLocationId.Value);

        var locationName = location != null
            ? $"{location.RefrigeratorName} - {location.ShelfNumber}"
            : null;

        return (previousLocationId, locationName);
    }

    public static string ResolveDiscardReason(ProcessBloodReturnRequest request, string returnReason)
    {
        var explicitReason = request.DiscardReason?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitReason))
        {
            return Truncate255(explicitReason);
        }

        if (request.Checklist != null)
        {
            var failedCriteria = GetFailedInspectionCriteria(request.Checklist);
            if (failedCriteria.Count > 0)
            {
                return Truncate255($"Không đạt kiểm định hoàn trả: {string.Join("; ", failedCriteria)}");
            }
        }

        return Truncate255($"Không đạt kiểm định tái nhập kho ({returnReason})");
    }

    public static string Truncate255(string value)
    {
        return value.Length <= 255 ? value : value[..255];
    }

    private static async Task<int?> FindLocationIdInAllocationLogsAsync(ApplicationDbContext context, int allocationId)
    {
        var oldStates = await context.AuditLogs
            .AsNoTracking()
            .Where(a =>
                a.TableName == "blood_allocations" &&
                a.RecordId == allocationId &&
                a.ActionType == ActionType.STATUS_CHANGE &&
                a.OldState != null)
            .OrderByDescending(a => a.LogId)
            .Select(a => a.OldState)
            .ToListAsync();

        foreach (var oldState in oldStates)
        {
            if (TryExtractLocationId(oldState, "locationId", out var locId))
            {
                return locId;
            }
        }

        return null;
    }

    private static async Task<int?> FindLocationIdInHandoverSessionLogsAsync(ApplicationDbContext context, int allocationId, int bagId)
    {
        var sessionIds = await context.BloodHandoverSessionItems
            .AsNoTracking()
            .Where(i => i.AllocationId == allocationId && i.BagId == bagId)
            .Select(i => i.HandoverSessionId)
            .ToListAsync();

        if (sessionIds.Count == 0)
        {
            return null;
        }

        var oldStates = await context.AuditLogs
            .AsNoTracking()
            .Where(a =>
                a.TableName == "blood_handover_sessions" &&
                sessionIds.Contains(a.RecordId) &&
                a.ActionType == ActionType.STATUS_CHANGE &&
                a.OldState != null)
            .OrderByDescending(a => a.LogId)
            .Select(a => a.OldState)
            .ToListAsync();

        foreach (var oldState in oldStates)
        {
            if (TryExtractBagLocationFromSessionState(oldState, bagId, out var locId))
            {
                return locId;
            }
        }

        return null;
    }

    private static bool TryExtractLocationId(string? json, string propertyName, out int locationId)
    {
        locationId = 0;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(propertyName, out var locProp) &&
                locProp.ValueKind == JsonValueKind.Number &&
                locProp.TryGetInt32(out locationId))
            {
                return true;
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }

    private static bool TryExtractBagLocationFromSessionState(string? json, int bagId, out int locationId)
    {
        locationId = 0;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("bags", out var bagsProp) || bagsProp.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var bagElement in bagsProp.EnumerateArray())
            {
                if (bagElement.TryGetProperty("bagId", out var idProp) &&
                    idProp.ValueKind == JsonValueKind.Number &&
                    idProp.TryGetInt32(out var currentBagId) &&
                    currentBagId == bagId &&
                    bagElement.TryGetProperty("locationId", out var locProp) &&
                    locProp.ValueKind == JsonValueKind.Number &&
                    locProp.TryGetInt32(out locationId))
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }

    public static List<string> GetFailedCreateChecklistCriteria(BloodReturnChecklistDto checklist, BloodComponentType? componentType = null)
    {
        var outsideMinutes = componentType?.MaxOutsideMinutesForRestock ?? 30;
        var failedCriteria = new List<string>();
        if (!checklist.IsUnusedFullVolume)
        {
            failedCriteria.Add("Chưa sử dụng, còn nguyên thể tích");
        }

        if (!checklist.IsBagIntact)
        {
            failedCriteria.Add("Túi máu còn nguyên vẹn");
        }

        if (!checklist.IsLabelClear)
        {
            failedCriteria.Add("Nhãn mác còn rõ ràng");
        }

        if (!checklist.IsStoredUnder30Mins)
        {
            failedCriteria.Add($"Thời gian ngoài tủ lạnh dưới {outsideMinutes} phút");
        }

        return failedCriteria;
    }

    private static List<string> GetFailedInspectionCriteria(BloodReturnInspectionChecklistDto checklist, BloodComponentType? componentType = null)
    {
        var outsideLimitLabel = componentType?.MaxOutsideMinutesForRestock is int maxMinutes
            ? $"Thời gian ngoài tủ lạnh hợp lệ (<= {maxMinutes} phút)"
            : "Thời gian ngoài tủ lạnh hợp lệ";

        var failed = new List<string>();
        if (!checklist.IsBagIntact)
        {
            failed.Add("Túi máu còn nguyên vẹn");
        }

        if (!checklist.IsLabelClear)
        {
            failed.Add("Nhãn mác còn rõ ràng");
        }

        if (!checklist.IsTempInRange)
        {
            failed.Add("Nhiệt độ bảo quản đạt chuẩn");
        }

        if (!checklist.IsOutsideTimeValid)
        {
            failed.Add(outsideLimitLabel);
        }

        if (!checklist.IsUnusedFullVolume)
        {
            failed.Add("Chưa sử dụng, còn nguyên thể tích");
        }

        return failed;
    }

    private static List<string> ExtractFailedChecklistFlags(string? json)
    {
        var flags = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return flags;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("failedChecklistFlags", out var arrProp) || arrProp.ValueKind != JsonValueKind.Array)
            {
                return flags;
            }

            foreach (var item in arrProp.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var text = item.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        flags.Add(text);
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return flags;
    }
}
