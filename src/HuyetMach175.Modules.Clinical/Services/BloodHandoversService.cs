using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodHandovers;
using HuyetMach175.Modules.Clinical.Mappings;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Services;

public class BloodHandoversService : IBloodHandoversService
{
    private readonly ApplicationDbContext _context;

    public BloodHandoversService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DispatchBloodBagResponse> DispatchBagAsync(DispatchBloodBagRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin kỹ thuật viên xuất kho không hợp lệ.");

        var barcode = request.Barcode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã vạch túi máu (barcode) không được để trống.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var normalizedBarcode = barcode.ToLower();
        var bag = await _context.BloodBags
            .Include(b => b.ComponentType)
            .FirstOrDefaultAsync(b => b.Barcode.ToLower() == normalizedBarcode);

        if (bag == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy túi máu có mã vạch '{barcode}'.");
        }

        if (bag.Status == BagStatus.ISSUED)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi máu {bag.Barcode} đã được xuất kho trước đó.");
        }

        if (bag.Status != BagStatus.RESERVED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đang ở trạng thái '{bag.Status}', chỉ có thể xuất kho túi máu đang được giữ chỗ (RESERVED).");
        }

        var now = DateTime.UtcNow;
        if (bag.ExpiredAt <= now)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {bag.Barcode} đã hết hạn sử dụng vào {bag.ExpiredAt:dd/MM/yyyy HH:mm}, không thể xuất kho.");
        }

        var allocation = await _context.BloodAllocations
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.Department)
            .FirstOrDefaultAsync(a => a.BagId == bag.BagId && a.Status == AllocationStatus.RESERVED);

        if (allocation == null || allocation.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy thông tin giữ chỗ (RESERVED) tương ứng cho túi máu {bag.Barcode}.");
        }

        if (allocation.Request.Status != RequestStatus.READY && allocation.Request.Status != RequestStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu {allocation.Request.RequestCode} đang ở trạng thái '{allocation.Request.Status}', không hợp lệ để xuất kho.");
        }

        var oldBagStatus = bag.Status;
        var oldLocationId = bag.LocationId;
        var oldAllocationStatus = allocation.Status;

        bag.Status = BagStatus.ISSUED;
        bag.LocationId = null;

        allocation.Status = AllocationStatus.ISSUED;
        allocation.IssuedBy = userId;
        allocation.IssuedAt = now;

        var auditLog = new AuditLog
        {
            TableName = "blood_allocations",
            RecordId = allocation.AllocationId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                allocationStatus = oldAllocationStatus.ToString(),
                bagId = bag.BagId,
                bagStatus = oldBagStatus.ToString(),
                locationId = oldLocationId
            }),
            NewState = JsonSerializer.Serialize(new
            {
                allocationStatus = allocation.Status.ToString(),
                bagId = bag.BagId,
                barcode = bag.Barcode,
                bagStatus = bag.Status.ToString(),
                issuedBy = userId,
                issuedAt = now,
                requestCode = allocation.Request.RequestCode
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Xuất kho túi máu {bag.Barcode} cho phiếu yêu cầu {allocation.Request.RequestCode}.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
        {
            BagId = bag.BagId,
            FromStatus = BagStatus.RESERVED,
            ToStatus = BagStatus.ISSUED,
            PerformedById = userId,
            AuditLogId = auditLog.LogId,
            Notes = $"Xuất kho cấp phát cho phiếu {allocation.Request.RequestCode}",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new DispatchBloodBagResponse
        {
            AllocationId = allocation.AllocationId,
            BagId = bag.BagId,
            Barcode = bag.Barcode,
            ComponentTypeId = bag.ComponentTypeId,
            ComponentCode = bag.ComponentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(bag.BloodType, bag.RhFactor),
            VolumeMl = bag.VolumeMl,
            ExpiredAt = bag.ExpiredAt,
            BagStatus = bag.Status,
            RequestId = allocation.Request.RequestId,
            RequestCode = allocation.Request.RequestCode,
            PatientCode = allocation.Request.PatientCode,
            PatientName = allocation.Request.PatientName,
            PatientBloodType = BloodCompatibilityRules.FormatBloodGroup(allocation.Request.PatientBloodType, allocation.Request.PatientRh),
            DepartmentName = allocation.Request.Doctor?.Department?.DepartmentName,
            DoctorName = allocation.Request.Doctor?.FullName,
            UrgencyLevel = allocation.Request.UrgencyLevel,
            RequestStatus = allocation.Request.Status,
            IssuedById = userId,
            IssuedByName = currentUser.FullName,
            IssuedAt = now
        };
    }

    public async Task<List<PendingReceiptBagDto>> GetPendingClinicalReceiptsAsync(PendingClinicalReceiptFilterDto filter, int userId)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người nhận không hợp lệ.");

        if (filter.DoctorId.HasValue && filter.DoctorId.Value <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã bác sĩ (doctorId) không hợp lệ.");
        }

        var departmentId = currentUser.DepartmentId;
        var query = _context.BloodAllocations
            .AsNoTracking()
            .Include(a => a.Bag)
                .ThenInclude(b => b!.ComponentType)
            .Include(a => a.IssuedByUser)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
                    .ThenInclude(d => d!.Department)
            .Where(a =>
                a.Status == AllocationStatus.ISSUED &&
                a.ReceivedBy == null &&
                a.Bag != null &&
                a.Bag.Status == BagStatus.ISSUED &&
                a.Request != null &&
                a.Request.Doctor != null &&
                a.Request.Doctor.DepartmentId == departmentId);

        if (filter.DoctorId.HasValue)
        {
            var doctorId = filter.DoctorId.Value;
            query = query.Where(a => a.Request!.DoctorId == doctorId);
        }

        if (!string.IsNullOrWhiteSpace(filter.DoctorName))
        {
            var doctorNameKw = filter.DoctorName.Trim();
            query = query.Where(a => EF.Functions.ILike(a.Request!.Doctor!.FullName, $"%{doctorNameKw}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.RequestCode))
        {
            var requestCodeKw = filter.RequestCode.Trim();
            query = query.Where(a => EF.Functions.ILike(a.Request!.RequestCode, $"%{requestCodeKw}%"));
        }

        var allocations = await query.ToListAsync();

        return allocations
            .Select(a => new PendingReceiptBagDto
            {
                AllocationId = a.AllocationId,
                RequestId = a.Request!.RequestId,
                RequestCode = a.Request.RequestCode,
                DoctorId = a.Request.DoctorId,
                DoctorName = a.Request.Doctor?.FullName,
                DepartmentName = a.Request.Doctor?.Department?.DepartmentName,
                IsOwnRequest = a.Request.DoctorId == userId,
                UrgencyLevel = a.Request.UrgencyLevel,
                PatientName = a.Request.PatientName,
                PatientCode = a.Request.PatientCode,
                BagId = a.Bag!.BagId,
                Barcode = a.Bag.Barcode,
                ComponentCode = a.Bag.ComponentType?.TypeCode ?? ComponentCode.WB,
                ComponentName = a.Bag.ComponentType?.TypeName ?? string.Empty,
                BloodType = BloodCompatibilityRules.FormatBloodGroup(a.Bag.BloodType, a.Bag.RhFactor),
                VolumeMl = a.Bag.VolumeMl,
                ExpiresAt = a.Bag.ExpiredAt,
                IssuedAt = a.IssuedAt,
                IssuedByName = a.IssuedByUser?.FullName
            })
            .OrderByDescending(x => x.IsOwnRequest)
            .ThenBy(x => BloodCompatibilityRules.GetUrgencyRank(x.UrgencyLevel))
            .ThenBy(x => x.IssuedAt ?? DateTime.MaxValue)
            .ThenBy(x => x.AllocationId)
            .ToList();
    }

    public async Task<ConfirmReceiptResponse> ConfirmClinicalReceiptAsync(ConfirmReceiptRequest request, int userId, string? ipAddress)
    {
        var currentUser = await BloodRequestProcessing.LoadActiveUserAsync(_context, userId, "Thông tin người nhận không hợp lệ.");

        if (request.AllocationId <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phân bổ (allocationId) không hợp lệ.");
        }

        var scannedBarcode = request.ScannedBarcode?.Trim() ?? string.Empty;
        var expectedPatientCode = request.ExpectedPatientCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(scannedBarcode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã vạch quét được (scannedBarcode) không được để trống.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var allocation = await _context.BloodAllocations
            .Include(a => a.Bag)
                .ThenInclude(b => b!.ComponentType)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Doctor)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Items)
            .Include(a => a.Request)
                .ThenInclude(r => r!.Allocations)
            .FirstOrDefaultAsync(a => a.AllocationId == request.AllocationId);

        if (allocation == null || allocation.Bag == null || allocation.Request == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy lượt phân bổ túi máu #{request.AllocationId}.");
        }

        BloodRequestProcessing.EnsureSameDepartmentAccess(
            currentUser,
            allocation.Request,
            "Bạn chỉ có quyền xác nhận nhận túi máu cho phiếu yêu cầu thuộc khoa của mình.");

        if (allocation.Status == AllocationStatus.COMPLETED || allocation.ReceivedBy.HasValue)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Túi máu {allocation.Bag.Barcode} (mã phân bổ #{allocation.AllocationId}) đã được xác nhận nhận trước đó.");
        }

        if (allocation.Status != AllocationStatus.ISSUED || allocation.Bag.Status != BagStatus.ISSUED)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Túi máu {allocation.Bag.Barcode} đang ở trạng thái '{allocation.Bag.Status}' (phân bổ '{allocation.Status}'), chưa sẵn sàng để xác nhận nhận tại lâm sàng.");
        }

        if (!string.Equals(allocation.Bag.Barcode, scannedBarcode, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Mã vạch quét được '{scannedBarcode}' không khớp với túi máu được phân bổ ({allocation.Bag.Barcode}).");
        }

        var isOwnRequest = allocation.Request.DoctorId == userId;
        if (!isOwnRequest && string.IsNullOrWhiteSpace(expectedPatientCode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Khi nhận hộ phiếu yêu cầu của bác sĩ khác, bắt buộc nhập mã người bệnh đối soát (expectedPatientCode).");
        }

        if (!string.IsNullOrWhiteSpace(expectedPatientCode) &&
            !string.Equals(allocation.Request.PatientCode, expectedPatientCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Mã người bệnh đối soát '{expectedPatientCode}' không khớp với bệnh nhân trên phiếu {allocation.Request.RequestCode}. Vui lòng kiểm tra lại đúng túi máu và người bệnh.");
        }

        var now = DateTime.UtcNow;
        var oldAllocationStatus = allocation.Status;
        var oldRequestStatus = allocation.Request.Status;

        allocation.ReceivedBy = userId;
        allocation.Status = AllocationStatus.COMPLETED;

        var allItemsAllocated = allocation.Request.Items.Count > 0 &&
                                allocation.Request.Items.All(i => i.AllocatedUnits >= i.RequestedUnits);
        var allActiveAllocationsCompleted = allocation.Request.Allocations
            .All(a => a.Status == AllocationStatus.COMPLETED || a.Status == AllocationStatus.RETURNED);

        if (allItemsAllocated && allActiveAllocationsCompleted)
        {
            allocation.Request.Status = RequestStatus.COMPLETED;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            TableName = "blood_allocations",
            RecordId = allocation.AllocationId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                allocationStatus = oldAllocationStatus.ToString(),
                requestStatus = oldRequestStatus.ToString(),
                receivedBy = (int?)null
            }),
            NewState = JsonSerializer.Serialize(new
            {
                allocationStatus = allocation.Status.ToString(),
                requestStatus = allocation.Request.Status.ToString(),
                receivedBy = userId,
                confirmedAt = now,
                barcode = allocation.Bag.Barcode,
                patientCode = allocation.Request.PatientCode
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Lâm sàng xác nhận thực nhận túi máu {allocation.Bag.Barcode} cho phiếu {allocation.Request.RequestCode}.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ConfirmReceiptResponse
        {
            AllocationId = allocation.AllocationId,
            RequestId = allocation.Request.RequestId,
            RequestCode = allocation.Request.RequestCode,
            PatientCode = allocation.Request.PatientCode,
            PatientName = allocation.Request.PatientName,
            BagId = allocation.Bag.BagId,
            Barcode = allocation.Bag.Barcode,
            ComponentCode = allocation.Bag.ComponentType?.TypeCode ?? ComponentCode.WB,
            ComponentName = allocation.Bag.ComponentType?.TypeName ?? string.Empty,
            BloodType = BloodCompatibilityRules.FormatBloodGroup(allocation.Bag.BloodType, allocation.Bag.RhFactor),
            VolumeMl = allocation.Bag.VolumeMl,
            AllocationStatus = allocation.Status,
            RequestStatus = allocation.Request.Status,
            CurrentStep = BloodRequestMappings.ResolveCurrentStep(allocation.Request.Status),
            ReceivedById = userId,
            ReceivedByName = currentUser.FullName,
            ConfirmedAt = now
        };
    }
}
