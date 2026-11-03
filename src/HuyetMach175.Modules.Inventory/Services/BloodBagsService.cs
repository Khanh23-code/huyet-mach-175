using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodBags;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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
