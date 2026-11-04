using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodBags;
using HuyetMach175.Modules.Inventory.DTOs.Common;
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
            throw new HttpException(StatusCodes.Status400BadRequest, $"Nhiệt độ tiếp nhận không đạt chuẩn an toàn ({request.ReceivedTemperature:0.#}°C). Nhiệt độ vận chuyển/tiếp nhận túi máu phải nằm trong khoảng từ 2.0°C đến 10.0°C.");
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
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu đang ở trạng thái '{bloodBag.Status}'. Chỉ túi máu mới thu gom ('NEWLY_COLLECTED') mới có thể tiếp nhận vào Viện.");
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
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{location.RefrigeratorName} - {location.ShelfNumber}' đang bị vô hiệu hóa hoặc bảo trì.");
        }

        if (location.IsFull)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{location.RefrigeratorName} - {location.ShelfNumber}' hiện đã đầy.");
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
            OldState = JsonSerializer.Serialize(new
            {
                status = oldStatus,
                locationId = oldLocationId
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = BagStatus.READY_TESTING.ToString(),
                locationId = location.LocationId,
                receivedTemperature = request.ReceivedTemperature
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Tiếp nhận túi máu về Viện. Nhiệt độ bàn giao: {request.ReceivedTemperature:0.#}°C. Vị trí lưu trữ: {location.RefrigeratorName} - {location.ShelfNumber}.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var bloodGroup = $"{(bloodBag.BloodType == BloodType.AB ? "AB" : bloodBag.BloodType.ToString())}{(bloodBag.RhFactor == RhFactor.POSITIVE ? "+" : "-")}";

        return new ReceiveBloodBagResponse
        {
            BagId = bloodBag.BagId,
            Barcode = bloodBag.Barcode,
            ComponentType = bloodBag.ComponentType?.TypeName ?? bloodBag.ComponentType?.TypeCode.ToString() ?? "Máu toàn phần",
            BloodType = bloodBag.BloodType.ToString(),
            RhFactor = bloodBag.RhFactor.ToString(),
            BloodGroup = bloodGroup,
            VolumeMl = bloodBag.VolumeMl,
            Status = bloodBag.Status.ToString(),
            StorageLocationId = location.LocationId,
            StorageLocationName = $"{location.RefrigeratorName} - {location.ShelfNumber}",
            ReceivedTemperature = request.ReceivedTemperature,
            CollectedAt = bloodBag.CollectedAt,
            ExpiredAt = bloodBag.ExpiredAt,
            ReceivedAt = now,
            ReceivedBy = userId,
            Message = $"Tiếp nhận túi máu {bloodBag.Barcode} thành công. Trạng thái chuyển sang READY_TESTING sẵn sàng xét nghiệm."
        };
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
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var statusStr = filter.Status.Trim().ToUpperInvariant();
            if (statusStr == "TESTING")
            {
                query = query.Where(b => b.Status == BagStatus.READY_TESTING);
            }
            else if (Enum.TryParse<BagStatus>(statusStr, true, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.BloodType))
        {
            var btStr = filter.BloodType.Trim().ToUpperInvariant();
            if (Enum.TryParse<BloodType>(btStr, true, out var parsedBloodType))
            {
                query = query.Where(b => b.BloodType == parsedBloodType);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.RhFactor))
        {
            var rhStr = filter.RhFactor.Trim().ToUpperInvariant();
            if (rhStr is "+" or "POS" or "POSITIVE")
            {
                query = query.Where(b => b.RhFactor == RhFactor.POSITIVE);
            }
            else if (rhStr is "-" or "NEG" or "NEGATIVE")
            {
                query = query.Where(b => b.RhFactor == RhFactor.NEGATIVE);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ComponentType))
        {
            var ctStr = filter.ComponentType.Trim().ToUpperInvariant();
            if (ctStr is "WHOLE_BLOOD" or "WB")
            {
                query = query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.WB);
            }
            else if (ctStr is "PACKED_RED_BLOOD_CELLS" or "PRBC")
            {
                query = query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.PRBC);
            }
            else if (ctStr is "FRESH_FROZEN_PLASMA" or "FFP")
            {
                query = query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.FFP);
            }
            else if (ctStr is "PLATELETS" or "PLT")
            {
                query = query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == ComponentCode.PLT);
            }
            else if (Enum.TryParse<ComponentCode>(ctStr, true, out var parsedCode))
            {
                query = query.Where(b => b.ComponentType != null && b.ComponentType.TypeCode == parsedCode);
            }
            else
            {
                query = query.Where(b => b.ComponentType != null && EF.Functions.ILike(b.ComponentType.TypeName, $"%{filter.ComponentType.Trim()}%"));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(b => EF.Functions.ILike(b.Barcode, $"%{kw}%"));
        }

        if (filter.LocationId.HasValue)
        {
            query = query.Where(b => b.LocationId == filter.LocationId.Value);
        }

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderByDescending(b => b.CollectedAt)
            .ThenByDescending(b => b.BagId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = entities.Select(b => new BloodBagDto
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
            BloodGroup = $"{(b.BloodType == BloodType.AB ? "AB" : b.BloodType.ToString())}{(b.RhFactor == RhFactor.POSITIVE ? "+" : "-")}",
            VolumeMl = b.VolumeMl,
            Status = b.Status.ToString(),
            StorageLocationId = b.LocationId,
            StorageLocationName = b.Location != null ? $"{b.Location.RefrigeratorName} - {b.Location.ShelfNumber}" : null,
            CollectedAt = b.CollectedAt,
            ExpiredAt = b.ExpiredAt,
            CreatedAt = b.CreatedAt
        }).ToList();

        return new PagedResult<BloodBagDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }
}
