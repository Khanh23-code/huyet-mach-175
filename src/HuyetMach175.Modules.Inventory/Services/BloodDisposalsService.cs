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
            .Include(r => r.BloodBag)
                .ThenInclude(b => b!.ComponentType)
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
                (r.BloodBag != null && EF.Functions.ILike(r.BloodBag.Barcode, $"%{kw}%")));
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
            .Include(r => r.BloodBag)
                .ThenInclude(b => b!.ComponentType)
            .Include(r => r.BloodBag)
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

    public async Task<DisposalDetailDto> CreateDisposalAsync(CreateDisposalRequest request, int userId, string? ipAddress)
    {
        if ((!request.BagId.HasValue || request.BagId.Value <= 0) && string.IsNullOrWhiteSpace(request.Barcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng cung cấp mã ID (bagId) hoặc mã vạch (barcode) của túi máu cần tiêu hủy.");
        }

        var normalizedBarcode = request.Barcode?.Trim();

        var bloodBag = await _context.BloodBags
            .Include(b => b.ComponentType)
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b =>
                (request.BagId.HasValue && request.BagId.Value > 0 && b.BagId == request.BagId.Value) ||
                (!string.IsNullOrEmpty(normalizedBarcode) && b.Barcode == normalizedBarcode));

        if (bloodBag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound,
                request.BagId.HasValue && request.BagId.Value > 0
                    ? $"Không tìm thấy túi máu với ID #{request.BagId.Value}."
                    : $"Không tìm thấy túi máu với mã barcode '{normalizedBarcode}'.");
        }

        if (bloodBag.Status == BagStatus.DISCARDED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu '{bloodBag.Barcode}' đã được tiêu hủy trước đó.");
        }

        if (bloodBag.Status is BagStatus.TRANSFUSED or BagStatus.ISSUED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu '{bloodBag.Barcode}' đang ở trạng thái '{bloodBag.Status}', không thể đề xuất tiêu hủy.");
        }

        var hasPendingDisposal = await _context.BloodDisposalRecords
            .AnyAsync(r => r.BagId == bloodBag.BagId && r.Status == DisposalStatus.PENDING_APPROVAL);

        if (hasPendingDisposal)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu '{bloodBag.Barcode}' hiện đã có phiếu đề xuất tiêu hủy đang chờ phê duyệt.");
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

        var previousBagStatus = bloodBag.Status;
        var now = DateTime.UtcNow;

        using var transaction = await _context.Database.BeginTransactionAsync();

        // Lock bag while pending approval
        bloodBag.Status = BagStatus.LOCKED;

        var record = new BloodDisposalRecord
        {
            DisposalCode = disposalCode,
            BagId = bloodBag.BagId,
            Reason = request.Reason,
            Notes = request.Notes?.Trim(),
            Status = DisposalStatus.PENDING_APPROVAL,
            PreviousStatus = previousBagStatus,
            RequestedBy = userId,
            RequestedAt = now
        };

        _context.BloodDisposalRecords.Add(record);
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
                bagId = bloodBag.BagId,
                barcode = bloodBag.Barcode,
                reason = record.Reason.ToString(),
                status = record.Status.ToString(),
                previousBagStatus = previousBagStatus.ToString()
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Lập phiếu đề xuất tiêu hủy {disposalCode} cho túi máu {bloodBag.Barcode} (Lý do: {BloodDisposalMappings.FormatDisposalReasonText(record.Reason)}).",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return await GetDisposalByIdAsync(record.DisposalId);
    }

    public async Task<DisposalDetailDto> CancelDisposalAsync(int id, CancelDisposalRequest? request, int userId, string? ipAddress)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu đề xuất tiêu hủy không hợp lệ.");
        }

        var record = await _context.BloodDisposalRecords
            .Include(r => r.BloodBag)
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
            throw new HttpException(StatusCodes.Status400BadRequest, "Không thể rút phiếu tiêu hủy khi túi máu đã được phê duyệt và hoàn tất tiêu hủy.");
        }

        if (record.Status == DisposalStatus.REJECTED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiếu tiêu hủy này đã bị Lãnh đạo từ chối, không thể thực hiện thao tác rút đơn.");
        }

        var now = DateTime.UtcNow;
        using var transaction = await _context.Database.BeginTransactionAsync();

        var oldDisposalStatus = record.Status;
        var oldBagStatus = record.BloodBag?.Status;

        record.Status = DisposalStatus.CANCELLED;

        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            var cancelNote = $"[Rút đơn vào {now:dd/MM/yyyy HH:mm}]: {request.Reason.Trim()}";
            record.Notes = string.IsNullOrWhiteSpace(record.Notes)
                ? cancelNote
                : $"{record.Notes}\n{cancelNote}";
        }

        if (record.BloodBag != null)
        {
            record.BloodBag.Status = record.PreviousStatus;
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_disposal_records",
            RecordId = record.DisposalId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                status = oldDisposalStatus.ToString(),
                bagStatus = oldBagStatus?.ToString()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = record.Status.ToString(),
                revertedBagStatus = record.PreviousStatus.ToString(),
                reason = request?.Reason
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Rút phiếu đề xuất tiêu hủy {record.DisposalCode}. Khôi phục trạng thái túi máu về {record.PreviousStatus}." +
                           (string.IsNullOrWhiteSpace(request?.Reason) ? "" : $" Lý do: {request.Reason.Trim()}"),
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return await GetDisposalByIdAsync(record.DisposalId);
    }
}