using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.DTOs.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class BloodProcessingService : IBloodProcessingService
{
    private readonly ApplicationDbContext _context;

    public BloodProcessingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProcessingQueueItemDto>> GetProcessingQueueAsync(ProcessingQueueFilterDto filter)
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
            .Include(b => b.Session)
                .ThenInclude(s => s!.Appointment)
                    .ThenInclude(a => a!.Donor)
            .Include(b => b.TestResults)
            .Where(b => b.Status == BagStatus.TESTED_PASSED &&
                        b.ComponentType != null &&
                        b.ComponentType.TypeCode == ComponentCode.WB &&
                        b.ParentBagId == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.Barcode, $"%{kw}%") ||
                (b.Session != null && b.Session.Appointment != null && b.Session.Appointment.Donor != null &&
                    (EF.Functions.ILike(b.Session.Appointment.Donor.FullName, $"%{kw}%") ||
                     EF.Functions.ILike(b.Session.Appointment.Donor.CitizenId, $"%{kw}%"))));
        }

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderBy(b => b.CollectedAt)
            .ThenBy(b => b.BagId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = entities.Select(b =>
        {
            var donor = b.Session?.Appointment?.Donor;
            var bloodGroup = $"{(b.BloodType == BloodType.AB ? "AB" : b.BloodType.ToString())}{(b.RhFactor == RhFactor.POSITIVE ? "+" : "-")}";
            var latestTest = b.TestResults.OrderByDescending(tr => tr.TestedAt).FirstOrDefault();

            return new ProcessingQueueItemDto
            {
                BagId = b.BagId,
                Barcode = b.Barcode,
                DonorCode = donor?.CitizenId,
                DonorName = donor != null ? donor.FullName : "Tiếp nhận ngoài viện",
                ComponentTypeCode = b.ComponentType?.TypeCode.ToString() ?? string.Empty,
                ComponentTypeName = b.ComponentType?.TypeName ?? string.Empty,
                VolumeMl = b.VolumeMl,
                BloodType = b.BloodType.ToString(),
                RhFactor = b.RhFactor.ToString(),
                BloodGroup = bloodGroup,
                StorageLocationId = b.LocationId,
                StorageLocationName = b.Location != null ? $"{b.Location.RefrigeratorName} - {b.Location.ShelfNumber}" : null,
                CollectedAt = b.CollectedAt,
                TestedAt = latestTest?.TestedAt,
                Status = b.Status.ToString()
            };
        }).ToList();

        return new PagedResult<ProcessingQueueItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<SeparateBloodBagResponse> SeparateBloodBagAsync(SeparateBloodBagRequest request, int userId, string? ipAddress)
    {
        var identifier = !string.IsNullOrWhiteSpace(request.ParentBarcode)
            ? request.ParentBarcode.Trim()
            : request.ParentBagId?.Trim();

        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã định danh túi máu mẹ (parentBarcode hoặc parentBagId) là bắt buộc.");
        }

        if (request.Components == null || request.Components.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Danh sách chế phẩm con cần phân tách không được để trống.");
        }

        int? numericId = int.TryParse(identifier, out var parsedId) ? parsedId : null;

        var parentBag = await _context.BloodBags
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b => b.Barcode == identifier || (numericId.HasValue && b.BagId == numericId.Value));

        if (parentBag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu với mã '{identifier}'.");
        }

        if (parentBag.Status == BagStatus.PROCESSED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu '{parentBag.Barcode}' đã được điều chế trước đó.");
        }

        if (parentBag.Status != BagStatus.TESTED_PASSED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu đang ở trạng thái '{parentBag.Status}', không hợp lệ để điều chế. Chỉ túi máu đã xét nghiệm đạt chuẩn ('TESTED_PASSED') mới được đưa vào phân tách chế phẩm.");
        }

        if (parentBag.ComponentType?.TypeCode != ComponentCode.WB)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Chỉ túi máu toàn phần (WB) mới có thể thực hiện ép tách thành các chế phẩm con.");
        }

        var componentTypes = await _context.BloodComponentTypes.ToListAsync();

        var requestedLocationIds = request.Components.Select(c => c.StorageLocationId).Distinct().ToList();
        var locations = await _context.StorageLocations
            .Where(l => requestedLocationIds.Contains(l.LocationId))
            .ToDictionaryAsync(l => l.LocationId);

        foreach (var comp in request.Components)
        {
            var mappedCode = MapComponentCode(comp.ComponentType);
            if (mappedCode == null)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Loại chế phẩm '{comp.ComponentType}' không hợp lệ. Các loại cho phép: RED_BLOOD_CELLS (PRBC), PLASMA (FFP), PLATELETS (PLT).");
            }

            if (!locations.TryGetValue(comp.StorageLocationId, out var loc))
            {
                throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy vị trí lưu trữ với ID {comp.StorageLocationId}.");
            }

            if (!loc.IsActive)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{loc.RefrigeratorName} - {loc.ShelfNumber}' đang bị vô hiệu hóa hoặc bảo trì.");
            }

            if (loc.IsFull)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Vị trí lưu trữ '{loc.RefrigeratorName} - {loc.ShelfNumber}' hiện đã đầy.");
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;

        var childBags = new List<BloodBag>();
        var derivedDtos = new List<DerivedComponentDto>();

        foreach (var item in request.Components)
        {
            var code = MapComponentCode(item.ComponentType)!.Value;
            var compType = componentTypes.FirstOrDefault(ct => ct.TypeCode == code);

            if (compType == null)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Hệ thống chưa thiết lập loại chế phẩm máu cho mã '{code}'.");
            }

            var loc = locations[item.StorageLocationId];
            var childBarcode = $"{parentBag.Barcode}-{code}";

            var existingBag = await _context.BloodBags.FirstOrDefaultAsync(b => b.Barcode == childBarcode);
            if (existingBag != null)
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Mã túi chế phẩm '{childBarcode}' đã tồn tại trong hệ thống.");
            }

            var expiredAt = parentBag.CollectedAt.AddDays(compType.LifeDays);

            var childBag = new BloodBag
            {
                Barcode = childBarcode,
                ParentBagId = parentBag.BagId,
                SessionId = parentBag.SessionId,
                ComponentTypeId = compType.ComponentTypeId,
                LocationId = loc.LocationId,
                BloodType = parentBag.BloodType,
                RhFactor = parentBag.RhFactor,
                VolumeMl = item.VolumeMl,
                CollectedAt = parentBag.CollectedAt,
                ExpiredAt = expiredAt,
                Status = BagStatus.AVAILABLE,
                CreatedAt = now
            };

            _context.BloodBags.Add(childBag);
            childBags.Add(childBag);

            var bloodGroup = $"{(childBag.BloodType == BloodType.AB ? "AB" : childBag.BloodType.ToString())}{(childBag.RhFactor == RhFactor.POSITIVE ? "+" : "-")}";

            derivedDtos.Add(new DerivedComponentDto
            {
                Barcode = childBarcode,
                ComponentTypeCode = compType.TypeCode.ToString(),
                ComponentTypeName = compType.TypeName,
                BloodType = childBag.BloodType.ToString(),
                RhFactor = childBag.RhFactor.ToString(),
                BloodGroup = bloodGroup,
                VolumeMl = childBag.VolumeMl,
                Status = childBag.Status.ToString(),
                StorageLocationId = loc.LocationId,
                StorageLocationName = $"{loc.RefrigeratorName} - {loc.ShelfNumber}",
                CollectedAt = childBag.CollectedAt,
                ExpiredAt = expiredAt
            });
        }

        var oldParentStatus = parentBag.Status.ToString();
        parentBag.Status = BagStatus.PROCESSED;
        parentBag.LocationId = null;

        await _context.SaveChangesAsync();

        for (int i = 0; i < childBags.Count; i++)
        {
            derivedDtos[i].BagId = childBags[i].BagId;
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_bags",
            RecordId = parentBag.BagId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                status = oldParentStatus,
                barcode = parentBag.Barcode
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = BagStatus.PROCESSED.ToString(),
                derivedBagCount = childBags.Count,
                derivedBarcodes = childBags.Select(cb => cb.Barcode).ToList()
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Hoàn tất điều chế. Túi máu toàn phần {parentBag.Barcode} đã được ép tách thành {childBags.Count} chế phẩm con: {string.Join(", ", childBags.Select(cb => cb.Barcode))}.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var technician = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        var technicianName = technician?.FullName ?? $"KTV #{userId}";

        return new SeparateBloodBagResponse
        {
            ParentBagId = parentBag.BagId,
            ParentBarcode = parentBag.Barcode,
            ParentStatus = parentBag.Status.ToString(),
            ProcessedAt = now,
            TechnicianId = userId,
            TechnicianName = technicianName,
            DerivedComponents = derivedDtos,
            Message = $"Điều chế túi máu {parentBag.Barcode} thành công. Đã sinh {childBags.Count} túi chế phẩm con sẵn sàng lưu kho và cấp phát."
        };
    }

    private static ComponentCode? MapComponentCode(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var clean = input.Trim().ToUpperInvariant();
        return clean switch
        {
            "RED_BLOOD_CELLS" or "PRBC" or "RBC" or "KHỐI HỒNG CẦU" or "HỒNG CẦU" => ComponentCode.PRBC,
            "PLASMA" or "FFP" or "HUYẾT TƯƠNG" or "HUYẾT TƯƠNG TƯƠI ĐÔNG LẠNH" => ComponentCode.FFP,
            "PLATELETS" or "PLT" or "TIỂU CẦU" or "KHỐI TIỂU CẦU" => ComponentCode.PLT,
            _ => Enum.TryParse<ComponentCode>(clean, true, out var parsed) ? parsed : null
        };
    }
}