using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
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
        BloodReturnInspectionChecklistDto? checklist,
        int? storageLocationId,
        DateTime nowUtc)
    {
        if (bag.ExpiredAt <= nowUtc)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đã hết hạn sử dụng vào {bag.ExpiredAt:dd/MM/yyyy HH:mm}, không thể tái nhập kho. Vui lòng chọn 'DISCARD'.");
        }

        if (checklist == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Bảng kiểm 5 điều kiện bảo quản (checklist) là bắt buộc khi nhập lại kho.");
        }

        var failedCriteria = GetFailedInspectionCriteria(checklist);
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

        if (!targetLocation.IsActive)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}' đang ngừng hoạt động.");
        }

        if (targetLocation.IsFull)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{targetLocation.RefrigeratorName} - {targetLocation.ShelfNumber}' hiện đã đầy.");
        }

        return targetLocation;
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

    private static List<string> GetFailedCreateChecklistCriteria(BloodReturnChecklistDto checklist)
    {
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
            failedCriteria.Add("Thời gian ngoài tủ lạnh dưới 30 phút");
        }

        return failedCriteria;
    }

    private static List<string> GetFailedInspectionCriteria(BloodReturnInspectionChecklistDto checklist)
    {
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
            failed.Add("Thời gian ngoài tủ lạnh hợp lệ");
        }

        if (!checklist.IsUnusedFullVolume)
        {
            failed.Add("Chưa sử dụng, còn nguyên thể tích");
        }

        return failed;
    }
}
