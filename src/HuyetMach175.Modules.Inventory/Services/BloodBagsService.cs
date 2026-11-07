using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodBags;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HuyetMach175.Modules.Inventory.Processing;

namespace HuyetMach175.Modules.Inventory.Services;

public class BloodBagsService : IBloodBagsService
{
    private readonly ApplicationDbContext _context;

    public BloodBagsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReceiveBloodBagResponse> ReceiveBloodBagAsync(ReceiveBloodBagRequest request, int userId, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã barcode túi máu không được để trống.");
        }

        if (request.ReceivedTemperature < 2.0m || request.ReceivedTemperature > 10.0m)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Nhiệt độ tiếp nhận không đạt chuẩn an toàn ({request.ReceivedTemperature:0.#}°C). Nhiệt độ vận chuyển/tiếp nhận túi máu phải nằm trong khoảng từ 2.0°C đến 10.0°C.");
        }

        var normalizedBarcode = request.Barcode.Trim();

        var bloodBag = await _context.BloodBags
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.Barcode == normalizedBarcode);

        if (bloodBag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu với mã barcode '{request.Barcode}'.");
        }

        if (bloodBag.Status != BagStatus.NEWLY_COLLECTED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Túi máu đang ở trạng thái '{bloodBag.Status}'. Chỉ túi máu mới thu gom ('NEWLY_COLLECTED') mới có thể tiếp nhận vào Viện.");
        }

        if (bloodBag.Session != null && bloodBag.Session.Status != SessionStatus.COMPLETED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Túi máu thuộc phiên lấy máu chưa hoàn tất, chưa thể tiếp nhận vào Viện.");
        }

        if (bloodBag.Session != null && bloodBag.Session.Status != SessionStatus.COMPLETED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Túi máu thuộc phiên lấy máu chưa hoàn tất, chưa thể tiếp nhận vào Viện.");
        }

        var location = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.LocationId == request.StorageLocationId);

        if (location == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với ID {request.StorageLocationId}.");
        }

        if (!location.IsActive)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Vị trí lưu trữ '{location.RefrigeratorName} - {location.ShelfNumber}' đang bị vô hiệu hóa hoặc bảo trì.");
        }

        if (location.IsFull)
        {
            throw new HttpException(StatusCodes.Status400BadRequest,
                $"Vị trí lưu trữ '{location.RefrigeratorName} - {location.ShelfNumber}' hiện đã đầy.");
        }

        var oldStatus = bloodBag.Status.ToString();
        var oldLocationId = bloodBag.LocationId;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        bloodBag.LocationId = location.LocationId;
        bloodBag.Status = BagStatus.READY_TESTING;

        var now = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            TableName = "blood_bags",
            RecordId = bloodBag.BagId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new { status = oldStatus, locationId = oldLocationId }),
            NewState = JsonSerializer.Serialize(new { status = BagStatus.READY_TESTING.ToString(), locationId = location.LocationId, receivedTemperature = request.ReceivedTemperature }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Tiếp nhận túi máu về Viện. Nhiệt độ bàn giao: {request.ReceivedTemperature:0.#}°C. Vị trí lưu trữ: {location.RefrigeratorName} - {location.ShelfNumber}.",
            CreatedAt = now
        };
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        var statusLog = new BloodBagStatusLog
        {
            BagId = bloodBag.BagId,
            FromStatus = BagStatus.NEWLY_COLLECTED,
            ToStatus = BagStatus.READY_TESTING,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = $"Tiếp nhận về Viện. Nhiệt độ: {request.ReceivedTemperature:0.#}°C",
            CreatedAt = now
        };
        _context.BloodBagStatusLogs.Add(statusLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return bloodBag.ToReceiveResponse(location, request.ReceivedTemperature, now, userId);
    }

    public async Task<RegisterExternalBloodBagResponse> RegisterExternalBloodBagAsync(RegisterExternalBloodBagRequest request, int userId, string? ipAddress)
    {
        var unitName = request.ExternalUnitName.Trim();
        var externalCode = request.ExternalCode.Trim();

        if (unitName.Length == 0 || externalCode.Length == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Đơn vị cung cấp và mã túi của đơn vị ngoài không được để trống.");
        }

        if (request.ReceivedTemperature < 2.0m || request.ReceivedTemperature > 10.0m)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Nhiệt độ tiếp nhận không đạt chuẩn an toàn ({request.ReceivedTemperature:0.#}°C). Phải nằm trong khoảng từ 2.0°C đến 10.0°C.");
        }

        var now = DateTime.UtcNow;
        var collectedAt = ToUtc(request.CollectedAt);

        if (collectedAt > now.AddMinutes(5))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Thời gian thu thập không được ở tương lai.");
        }

        var wb = await _context.BloodComponentTypes
            .FirstOrDefaultAsync(ct => ct.TypeCode == ComponentCode.WB);

        if (wb == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Hệ thống chưa thiết lập loại chế phẩm máu toàn phần (WB).");
        }

        var expiredAt = collectedAt.AddDays(wb.LifeDays);
        if (expiredAt <= now)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Túi máu đã quá hạn sử dụng theo thời gian thu thập, không thể tiếp nhận.");
        }

        var location = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.LocationId == request.StorageLocationId);

        if (location == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với ID {request.StorageLocationId}.");
        }

        var locationName = $"{location.RefrigeratorName} - {location.ShelfNumber}";

        if (!location.IsActive)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{locationName}' đang bị vô hiệu hóa hoặc bảo trì.");
        }

        if (location.IsFull)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{locationName}' hiện đã đầy.");
        }

        if (!BagRules.IsStorageSuitable(ComponentCode.WB, location.TargetTemperature))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí '{locationName}' ({location.TargetTemperature:0.#}°C) không phù hợp để bảo quản máu toàn phần (cần {BagRules.StorageHint(ComponentCode.WB)}).");
        }

        var unitKey = unitName.ToLower();
        var codeKey = externalCode.ToLower();
        var duplicated = await _context.BloodBags.AnyAsync(b =>
            b.ExternalUnitName != null && b.ExternalCode != null &&
            b.ExternalUnitName.ToLower() == unitKey &&
            b.ExternalCode.ToLower() == codeKey);

        if (duplicated)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi mã '{externalCode}' của đơn vị '{unitName}' đã được tiếp nhận trước đó.");
        }

        var bloodType = request.BloodType!.Value;
        var rhFactor = request.RhFactor!.Value;

        var bag = new BloodBag
        {
            // Barcode tạm, thay bằng EXT-... ngay sau khi có BagId (cùng transaction).
            Barcode = $"EXT-TMP-{Guid.NewGuid():N}",
            SessionId = null,
            ParentBagId = null,
            ComponentTypeId = wb.ComponentTypeId,
            LocationId = location.LocationId,
            BloodType = bloodType,
            RhFactor = rhFactor,
            VolumeMl = request.VolumeMl,
            CollectedAt = collectedAt,
            ExpiredAt = expiredAt,
            Status = BagStatus.READY_TESTING,
            ExternalUnitName = unitName,
            ExternalCode = externalCode,
            CreatedAt = now
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.BloodBags.Add(bag);
            await _context.SaveChangesAsync();

            bag.Barcode = BagRules.ExternalBarcode(collectedAt, bag.BagId);

            var bloodGroup = InventoryProcessing.FormatBloodGroup(bloodType, rhFactor);

            _context.AuditLogs.Add(new AuditLog
            {
                TableName = "blood_bags",
                RecordId = bag.BagId,
                ActionType = ActionType.INSERT,
                OldState = JsonSerializer.Serialize(new { }),
                NewState = JsonSerializer.Serialize(new
                {
                    barcode = bag.Barcode,
                    status = BagStatus.READY_TESTING.ToString(),
                    bloodGroup,
                    volumeMl = bag.VolumeMl,
                    externalUnitName = unitName,
                    externalCode,
                    locationId = location.LocationId,
                    receivedTemperature = request.ReceivedTemperature
                }),
                PerformedBy = userId,
                IpAddress = ipAddress,
                ChangeReason = $"Tiếp nhận túi máu ngoài từ '{unitName}' (mã gốc: {externalCode}). Nhiệt độ bàn giao: {request.ReceivedTemperature:0.#}°C. Vị trí lưu trữ: {locationName}.",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new RegisterExternalBloodBagResponse
            {
                BagId = bag.BagId,
                Barcode = bag.Barcode,
                ComponentTypeName = wb.TypeName,
                BloodGroup = bloodGroup,
                VolumeMl = bag.VolumeMl,
                Status = bag.Status.ToString(),
                ExternalUnitName = unitName,
                ExternalCode = externalCode,
                StorageLocationId = location.LocationId,
                StorageLocationName = locationName,
                ReceivedTemperature = request.ReceivedTemperature,
                CollectedAt = bag.CollectedAt,
                ExpiredAt = bag.ExpiredAt,
                ReceivedAt = now,
                ReceivedBy = userId,
                Message = $"Đã khởi tạo mã {bag.Barcode}. Túi máu ở trạng thái READY_TESTING, sẵn sàng xét nghiệm."
            };
        }
        catch (DbUpdateException)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Không thể lưu túi máu: mã '{externalCode}' của đơn vị '{unitName}' có thể vừa được tiếp nhận bởi người khác.");
        }
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public async Task<PagedResult<BloodBagDto>> GetBloodBagsAsync(BloodBagFilterDto filter)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.BloodBags
            .AsNoTracking()
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .Include(b => b.ParentBag)
            .ApplyFilter(filter);

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderByDescending(b => b.CollectedAt)
            .ThenByDescending(b => b.BagId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<BloodBagDto>
        {
            Items = entities.Select(b => b.ToDto()).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<BloodBagScanResponse> ScanBloodBagAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã barcode túi máu không được để trống.");
        }

        var normalizedBarcode = barcode.Trim();

        var bag = await _context.BloodBags
            .AsNoTracking()
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b => b.Barcode == normalizedBarcode);

        if (bag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu với mã barcode '{barcode}'.");
        }

        var logs = await _context.BloodBagStatusLogs
            .AsNoTracking()
            .Where(l => l.BagId == bag.BagId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        return bag.ToScanResponse(logs);
    }

    public async Task<TransitionBagStatusResponse> TransitionStatusAsync(TransitionBagStatusRequest request, int userId, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã barcode túi máu không được để trống.");
        }

        var normalizedBarcode = request.Barcode.Trim();

        var bag = await _context.BloodBags
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b => b.Barcode == normalizedBarcode);

        if (bag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu với mã barcode '{request.Barcode}'.");
        }

        var now = DateTime.UtcNow;

        var hasActiveDisposal = false;
        if (bag.Status == BagStatus.LOCKED)
        {
            hasActiveDisposal = await _context.BloodDisposalItems
                .Include(i => i.DisposalRecord)
                .AnyAsync(i => i.BagId == bag.BagId && i.DisposalRecord != null &&
                               (i.DisposalRecord.Status == DisposalStatus.PENDING_APPROVAL || i.DisposalRecord.Status == DisposalStatus.APPROVED));
        }

        // Validate state machine guardrails
        BloodBagTransitionRules.ValidateTransition(bag, request.TargetStatus, hasActiveDisposal, now);

        // Validate storage location if specified
        StorageLocation? newLocation = null;
        if (request.StorageLocationId.HasValue)
        {
            newLocation = await _context.StorageLocations.FirstOrDefaultAsync(l => l.LocationId == request.StorageLocationId.Value);
            if (newLocation == null)
            {
                throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với ID {request.StorageLocationId.Value}.");
            }
            if (!newLocation.IsActive)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{newLocation.RefrigeratorName} - {newLocation.ShelfNumber}' đang bị vô hiệu hóa hoặc bảo trì.");
            }
            if (newLocation.IsFull && bag.LocationId != newLocation.LocationId)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{newLocation.RefrigeratorName} - {newLocation.ShelfNumber}' hiện đã đầy.");
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var oldStatus = bag.Status;
        var oldLocationId = bag.LocationId;

        bag.Status = request.TargetStatus;

        if (request.StorageLocationId.HasValue)
        {
            bag.LocationId = request.StorageLocationId.Value;
            bag.Location = newLocation;
        }
        else if (request.TargetStatus is BagStatus.ISSUED or BagStatus.TRANSFUSED or BagStatus.DISCARDED)
        {
            bag.LocationId = null;
            bag.Location = null;
        }

        // 1. Dual-Logging: System-wide audit log
        var auditLog = new AuditLog
        {
            TableName = "blood_bags",
            RecordId = bag.BagId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new { status = oldStatus.ToString(), locationId = oldLocationId }),
            NewState = JsonSerializer.Serialize(new { status = bag.Status.ToString(), locationId = bag.LocationId }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Chuyển trạng thái túi máu {bag.Barcode} từ {BloodBagMappings.FormatLifecycleStatusLabel(oldStatus)} sang {BloodBagMappings.FormatLifecycleStatusLabel(bag.Status)}." +
                           (string.IsNullOrWhiteSpace(request.Notes) ? "" : $" Ghi chú: {request.Notes.Trim()}"),
            CreatedAt = now
        };
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        // 2. Dual-Logging: Lifecycle status log linked to audit log
        var statusLog = new BloodBagStatusLog
        {
            BagId = bag.BagId,
            FromStatus = oldStatus,
            ToStatus = bag.Status,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = request.Notes?.Trim(),
            CreatedAt = now
        };
        _context.BloodBagStatusLogs.Add(statusLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new TransitionBagStatusResponse
        {
            Barcode = bag.Barcode,
            PreviousStatus = oldStatus.ToString(),
            PreviousStatusLabel = BloodBagMappings.FormatLifecycleStatusLabel(oldStatus),
            CurrentStatus = bag.Status.ToString(),
            CurrentStatusLabel = BloodBagMappings.FormatLifecycleStatusLabel(bag.Status),
            StorageLocation = bag.Location != null
                ? new StorageLocationSummaryDto
                {
                    LocationId = bag.Location.LocationId,
                    LocationName = $"{bag.Location.RefrigeratorName} - {bag.Location.ShelfNumber}"
                }
                : null,
            UpdatedAt = now,
            Message = $"Cập nhật trạng thái túi máu thành công sang '{BloodBagMappings.FormatLifecycleStatusLabel(bag.Status)}'."
        };
    }

    public async Task<RecentStatusLogsResponse> GetRecentStatusLogsAsync(int limit)
    {
        var clampedLimit = Math.Clamp(limit <= 0 ? 10 : limit, 1, 100);

        var logs = await _context.BloodBagStatusLogs
            .AsNoTracking()
            .Include(l => l.BloodBag)
            .Include(l => l.PerformedByUser)
            .OrderByDescending(l => l.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync();

        return new RecentStatusLogsResponse
        {
            Total = logs.Count,
            Items = logs.Select(l => l.ToRecentLogDto()).ToList()
        };
    }
}