using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Inventory.DTOs.BloodDisposals;
using HuyetMach175.Modules.Inventory.DTOs.Common;
using HuyetMach175.Modules.Inventory.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Inventory.Services;

public class BloodDisposalsService : IBloodDisposalsService
{
    private readonly ApplicationDbContext _context;

    public BloodDisposalsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DisposalRecordDto>> GetDisposalsAsync(DisposalFilterDto filter)
    {
        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

        var query = _context.BloodDisposalRecords
            .AsNoTracking()
            .Include(r => r.Items)
                .ThenInclude(i => i.BloodBag)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<DisposalStatus>(filter.Status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Reason))
        {
            if (Enum.TryParse<DisposalReason>(filter.Reason.Trim(), true, out var parsedReason))
            {
                query = query.Where(r => r.Reason == parsedReason);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(r =>
                EF.Functions.ILike(r.DisposalCode, $"%{kw}%") ||
                r.Items.Any(i => i.BloodBag != null && EF.Functions.ILike(i.BloodBag.Barcode, $"%{kw}%")));
        }

        if (filter.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.FromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(r => r.RequestedAt >= fromUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(r => r.RequestedAt <= toUtc);
        }

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderByDescending(r => r.RequestedAt)
            .ThenByDescending(r => r.DisposalId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = records.Select(r => r.ToRecordDto()).ToList();

        return new PagedResult<DisposalRecordDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<DisposalDetailDto> GetDisposalByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu đề xuất tiêu hủy không hợp lệ.");
        }

        var r = await _context.BloodDisposalRecords
            .AsNoTracking()
            .Include(r => r.Items)
                .ThenInclude(i => i.BloodBag)
                    .ThenInclude(b => b!.ComponentType)
            .Include(r => r.Items)
                .ThenInclude(i => i.BloodBag)
                    .ThenInclude(b => b!.Location)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .FirstOrDefaultAsync(r => r.DisposalId == id);

        if (r == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu đề xuất tiêu hủy với mã #{id}.");
        }

        return r.ToDetailDto();
    }

    public async Task<CreateDisposalResponse> CreateDisposalAsync(CreateDisposalRequest request, int userId, string? ipAddress)
    {
        if (request.Barcodes == null || request.Barcodes.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng chọn ít nhất 1 mã vạch (barcode) túi máu cần tiêu hủy.");
        }

        var rawBarcodes = request.Barcodes
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => b.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rawBarcodes.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Danh sách mã vạch (barcode) không hợp lệ.");
        }

        var bags = await _context.BloodBags
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .Where(b => rawBarcodes.Contains(b.Barcode))
            .ToListAsync();

        var foundBarcodes = bags.Select(b => b.Barcode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = rawBarcodes.Where(b => !foundBarcodes.Contains(b)).ToList();
        if (missing.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy các túi máu với mã barcode: {string.Join(", ", missing)}.");
        }

        var discarded = bags.Where(b => b.Status == BagStatus.DISCARDED).Select(b => b.Barcode).ToList();
        if (discarded.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Các túi máu sau đã được tiêu hủy trước đó: {string.Join(", ", discarded)}.");
        }

        var invalidStatus = bags.Where(b => b.Status is BagStatus.TRANSFUSED or BagStatus.ISSUED).Select(b => $"{b.Barcode} ({b.Status})").ToList();
        if (invalidStatus.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Không thể đề xuất tiêu hủy các túi máu đang ở trạng thái: {string.Join(", ", invalidStatus)}.");
        }

        var bagIds = bags.Select(b => b.BagId).ToList();
        var pendingItems = await _context.BloodDisposalItems
            .Include(i => i.DisposalRecord)
            .Include(i => i.BloodBag)
            .Where(i => bagIds.Contains(i.BagId) && i.DisposalRecord != null && i.DisposalRecord.Status == DisposalStatus.PENDING_APPROVAL)
            .ToListAsync();

        if (pendingItems.Count > 0)
        {
            var conflictBarcodes = pendingItems.Select(i => i.BloodBag?.Barcode ?? i.BagId.ToString()).Distinct().ToList();
            throw new HttpException(StatusCodes.Status400BadRequest, $"Các túi máu sau hiện đã nằm trong biên bản tiêu hủy đang chờ duyệt: {string.Join(", ", conflictBarcodes)}.");
        }

        var todayPrefix = $"DSP-{DateTime.UtcNow:yyyyMMdd}-";
        var countToday = await _context.BloodDisposalRecords
            .CountAsync(r => r.DisposalCode.StartsWith(todayPrefix));
        var seq = countToday + 1;
        var disposalCode = $"{todayPrefix}{seq:D4}";

        while (await _context.BloodDisposalRecords.AnyAsync(r => r.DisposalCode == disposalCode))
        {
            seq++;
            disposalCode = $"{todayPrefix}{seq:D4}";
        }

        var now = DateTime.UtcNow;
        using var transaction = await _context.Database.BeginTransactionAsync();

        var record = new BloodDisposalRecord
        {
            DisposalCode = disposalCode,
            Reason = request.Reason,
            Notes = request.Notes?.Trim(),
            Status = DisposalStatus.PENDING_APPROVAL,
            TotalBags = bags.Count,
            RequestedBy = userId,
            RequestedAt = now
        };

        _context.BloodDisposalRecords.Add(record);
        await _context.SaveChangesAsync();

        foreach (var bag in bags)
        {
            var prevStatus = bag.Status;
            bag.Status = BagStatus.LOCKED;

            var item = new BloodDisposalItem
            {
                DisposalId = record.DisposalId,
                BagId = bag.BagId,
                PreviousStatus = prevStatus,
                IsVerified = true,
                VerifiedAt = now
            };
            _context.BloodDisposalItems.Add(item);
        }

        await _context.SaveChangesAsync();

        var auditLog = new AuditLog
        {
            TableName = "blood_disposal_records",
            RecordId = record.DisposalId,
            ActionType = ActionType.INSERT,
            OldState = null,
            NewState = JsonSerializer.Serialize(new
            {
                disposalId = record.DisposalId,
                disposalCode = record.DisposalCode,
                totalBags = bags.Count,
                barcodes = bags.Select(b => b.Barcode).ToList(),
                reason = record.Reason.ToString(),
                status = record.Status.ToString()
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Lập biên bản đề xuất tiêu hủy {disposalCode} cho lô {bags.Count} túi máu (Lý do: {BloodDisposalMappings.FormatDisposalReasonText(record.Reason)}).",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new CreateDisposalResponse
        {
            DisposalId = record.DisposalId,
            DisposalCode = record.DisposalCode,
            TotalBags = bags.Count,
            Reason = record.Reason.ToString(),
            ReasonText = BloodDisposalMappings.FormatDisposalReasonText(record.Reason),
            Status = record.Status.ToString(),
            StatusText = BloodDisposalMappings.FormatDisposalStatusText(record.Status),
            RequestedAt = record.RequestedAt,
            Message = $"Lập biên bản đề xuất tiêu hủy thành công cho lô {bags.Count} túi máu."
        };
    }

    public async Task<DisposalDetailDto> CancelDisposalAsync(int id, CancelDisposalRequest? request, int userId, string? ipAddress)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu đề xuất tiêu hủy không hợp lệ.");
        }

        var record = await _context.BloodDisposalRecords
            .Include(r => r.Items)
                .ThenInclude(i => i.BloodBag)
            .FirstOrDefaultAsync(r => r.DisposalId == id);

        if (record == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu đề xuất tiêu hủy với mã #{id}.");
        }

        if (record.Status == DisposalStatus.CANCELLED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiếu đề xuất tiêu hủy này đã được rút/hủy trước đó.");
        }

        if (record.Status == DisposalStatus.DISPOSED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Không thể rút phiếu tiêu hủy khi lô túi máu đã được phê duyệt và hoàn tất tiêu hủy.");
        }

        if (record.Status == DisposalStatus.REJECTED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiếu tiêu hủy này đã bị Lãnh đạo từ chối, không thể thực hiện thao tác rút đơn.");
        }

        var now = DateTime.UtcNow;
        using var transaction = await _context.Database.BeginTransactionAsync();

        var oldDisposalStatus = record.Status;

        record.Status = DisposalStatus.CANCELLED;

        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            var cancelNote = $"[Rút đơn vào {now:dd/MM/yyyy HH:mm}]: {request.Reason.Trim()}";
            record.Notes = string.IsNullOrWhiteSpace(record.Notes)
                ? cancelNote
                : $"{record.Notes}\n{cancelNote}";
        }

        // Revert all blood bags in this disposal batch
        foreach (var item in record.Items)
        {
            if (item.BloodBag != null)
            {
                item.BloodBag.Status = item.PreviousStatus;
            }
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_disposal_records",
            RecordId = record.DisposalId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                status = oldDisposalStatus.ToString()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = record.Status.ToString(),
                totalBagsReverted = record.Items.Count,
                reason = request?.Reason
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Rút biên bản đề xuất tiêu hủy {record.DisposalCode}. Khôi phục trạng thái cho {record.Items.Count} túi máu." +
                           (string.IsNullOrWhiteSpace(request?.Reason) ? "" : $" Lý do: {request.Reason.Trim()}"),
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return await GetDisposalByIdAsync(record.DisposalId);
    }
}
