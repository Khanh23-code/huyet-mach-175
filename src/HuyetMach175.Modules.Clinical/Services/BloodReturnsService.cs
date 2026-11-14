using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodReturns;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Services;

public class BloodReturnsService : IBloodReturnsService
{
    private readonly ApplicationDbContext _context;

    public BloodReturnsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateBloodReturnResponse> CreateBloodReturnAsync(CreateBloodReturnRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người lập phiếu hoàn trả không hợp lệ.");

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

        ValidateReturnChecklist(request.Checklist);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var allocation = await _context.BloodAllocations
            .Include(a => a.Bag)
                .ThenInclude(b => b!.ComponentType)
            .Include(a => a.BloodReturn)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
            .FirstOrDefaultAsync(a => a.AllocationId == request.AllocationId);

        if (allocation == null || allocation.Bag == null || allocation.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lượt phân bổ túi máu #{request.AllocationId}.");
        }

        BloodRequestProcessing.EnsureSameDepartmentAccess(
            currentUser,
            allocation.Request,
            "Bạn chỉ có quyền lập phiếu hoàn trả cho túi máu thuộc khoa của mình.");

        if (allocation.BloodReturn != null)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Lượt phân bổ #{allocation.AllocationId} (túi {allocation.Bag.Barcode}) đã được lập phiếu hoàn trả trước đó (#{allocation.BloodReturn.ReturnId}).");
        }

        if (allocation.Bag.Status != BagStatus.ISSUED ||
            (allocation.Status != AllocationStatus.ISSUED && allocation.Status != AllocationStatus.COMPLETED))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {allocation.Bag.Barcode} đang ở trạng thái '{allocation.Bag.Status}' (phân bổ '{allocation.Status}'), chỉ có thể lập phiếu hoàn trả cho túi đã xuất kho.");
        }

        var now = DateTime.UtcNow;
        var oldBagStatus = allocation.Bag.Status;

        var bloodReturn = new BloodReturn
        {
            AllocationId = allocation.AllocationId,
            ReturnedBy = userId,
            ReceivedBy = userId,
            ReturnReason = returnReason,
            Qualified = false,
            FinalAction = ReturnFinalAction.PENDING_INSPECTION,
            ProcessedAt = now
        };

        allocation.Bag.Status = BagStatus.LOCKED;

        _context.BloodReturns.Add(bloodReturn);
        await _context.SaveChangesAsync();

        var auditLog = new AuditLog
        {
            TableName = "blood_returns",
            RecordId = bloodReturn.ReturnId,
            ActionType = ActionType.INSERT,
            OldState = JsonSerializer.Serialize(new
            {
                bagId = allocation.Bag.BagId,
                bagStatus = oldBagStatus.ToString()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                returnId = bloodReturn.ReturnId,
                allocationId = allocation.AllocationId,
                bagId = allocation.Bag.BagId,
                barcode = allocation.Bag.Barcode,
                bagStatus = allocation.Bag.Status.ToString(),
                finalAction = bloodReturn.FinalAction.ToString(),
                qualified = bloodReturn.Qualified,
                returnReason,
                checklist = request.Checklist
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Lâm sàng lập phiếu hoàn trả #{bloodReturn.ReturnId} cho túi máu {allocation.Bag.Barcode}.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = allocation.Bag.BagId,
            FromStatus = BagStatus.ISSUED,
            ToStatus = BagStatus.LOCKED,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = $"Phong tỏa túi máu do lập phiếu hoàn trả #{bloodReturn.ReturnId}: {returnReason}",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CreateBloodReturnResponse
        {
            ReturnId = bloodReturn.ReturnId,
            AllocationId = allocation.AllocationId,
            RequestId = allocation.Request.RequestId,
            RequestCode = allocation.Request.RequestCode,
            PatientCode = allocation.Request.PatientCode,
            PatientName = allocation.Request.PatientName,
            BagId = allocation.Bag.BagId,
            Barcode = allocation.Bag.Barcode,
            ComponentName = allocation.Bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(allocation.Bag.BloodType, allocation.Bag.RhFactor),
            ReturnReason = bloodReturn.ReturnReason,
            Qualified = bloodReturn.Qualified,
            FinalAction = bloodReturn.FinalAction,
            BagStatus = allocation.Bag.Status,
            ReturnedById = userId,
            ReturnedByName = currentUser.FullName,
            ProcessedAt = now
        };
    }

    private static void ValidateReturnChecklist(BloodReturnChecklistDto? checklist)
    {
        if (checklist == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Bảng kiểm điều kiện hoàn trả (checklist) không được để trống.");
        }

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

        if (failedCriteria.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu không đạt đủ 4 điều kiện hoàn trả bắt buộc: {string.Join("; ", failedCriteria)}.");
        }
    }
}
