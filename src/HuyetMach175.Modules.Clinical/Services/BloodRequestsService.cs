using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Mappings;
using HuyetMach175.Modules.Clinical.Processing;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Services;

public class BloodRequestsService : IBloodRequestsService
{
    private readonly ApplicationDbContext _context;

    public BloodRequestsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateBloodRequestResponse> CreateBloodRequestAsync(CreateBloodRequestDto request, int doctorId, string? ipAddress)
    {
        await BloodRequestProcessing.EnsureActiveUserAsync(_context, doctorId, "Thông tin bác sĩ đăng nhập không hợp lệ.");

        var (patientCode, patientName) = BloodRequestProcessing.ValidateCreateBloodRequest(request);

        var componentTypeIds = request.Items.Select(i => i.ComponentTypeId).Distinct().ToList();
        var existingComponentIds = await _context.BloodComponentTypes
            .AsNoTracking()
            .Where(c => componentTypeIds.Contains(c.ComponentTypeId))
            .Select(c => c.ComponentTypeId)
            .ToHashSetAsync();

        var missingComponentIds = componentTypeIds.Where(id => !existingComponentIds.Contains(id)).ToList();
        if (missingComponentIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy loại chế phẩm máu với ID: {string.Join(", ", missingComponentIds)}.");
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var requestCode = await BloodRequestProcessing.GenerateRequestCodeAsync(_context, now);
        var status = request.IsDraft ? RequestStatus.DRAFT : RequestStatus.PENDING;

        var entity = new BloodRequest
        {
            RequestCode = requestCode,
            DoctorId = doctorId,
            PatientCode = patientCode,
            PatientName = patientName,
            PatientBloodType = request.PatientBloodType,
            PatientRh = request.PatientRh,
            UrgencyLevel = request.UrgencyLevel,
            Diagnosis = request.Diagnosis?.Trim(),
            Status = status,
            CreatedAt = now
        };

        _context.BloodRequests.Add(entity);
        await _context.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            _context.BloodRequestItems.Add(new BloodRequestItem
            {
                RequestId = entity.RequestId,
                ComponentTypeId = item.ComponentTypeId,
                RequestedUnits = item.RequestedUnits,
                AllocatedUnits = 0
            });
        }

        var auditLog = new AuditLog
        {
            TableName = "blood_requests",
            RecordId = entity.RequestId,
            ActionType = ActionType.INSERT,
            OldState = null,
            NewState = JsonSerializer.Serialize(new
            {
                requestId = entity.RequestId,
                requestCode = entity.RequestCode,
                doctorId = entity.DoctorId,
                patientCode = entity.PatientCode,
                patientName = entity.PatientName,
                patientBloodType = entity.PatientBloodType.ToString(),
                patientRh = entity.PatientRh.ToString(),
                urgencyLevel = entity.UrgencyLevel.ToString(),
                status = entity.Status.ToString(),
                totalRequestedUnits = request.Items.Sum(i => i.RequestedUnits)
            }),
            PerformedBy = doctorId,
            IpAddress = ipAddress,
            ChangeReason = request.IsDraft
                ? $"Lưu nháp phiếu yêu cầu máu {entity.RequestCode} cho người bệnh {entity.PatientName} ({entity.PatientCode})."
                : $"Tạo và gửi phiếu yêu cầu máu {entity.RequestCode} cho người bệnh {entity.PatientName} ({entity.PatientCode}).",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return new CreateBloodRequestResponse
        {
            RequestId = entity.RequestId,
            RequestCode = entity.RequestCode,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<BloodRequestDetailDto> UpdateDraftBloodRequestAsync(int id, UpdateBloodRequestDto request, int doctorId, string? ipAddress)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu máu không hợp lệ.");
        }

        await BloodRequestProcessing.EnsureActiveUserAsync(_context, doctorId, "Thông tin bác sĩ đăng nhập không hợp lệ.");

        var (patientCode, patientName) = BloodRequestProcessing.ValidateUpdateBloodRequest(request);

        var componentTypeIds = request.Items.Select(i => i.ComponentTypeId).Distinct().ToList();
        var existingComponentIds = await _context.BloodComponentTypes
            .AsNoTracking()
            .Where(c => componentTypeIds.Contains(c.ComponentTypeId))
            .Select(c => c.ComponentTypeId)
            .ToHashSetAsync();

        var missingComponentIds = componentTypeIds.Where(cid => !existingComponentIds.Contains(cid)).ToList();
        if (missingComponentIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy loại chế phẩm máu với ID: {string.Join(", ", missingComponentIds)}.");
        }

        var now = DateTime.UtcNow;
        var diagnosisTrimmed = request.Diagnosis?.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var existingRequest = await _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (existingRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{id}.");
        }

        if (existingRequest.DoctorId != doctorId)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "Bạn chỉ có quyền chỉnh sửa phiếu yêu cầu máu nháp do chính mình tạo.");
        }

        if (existingRequest.Status != RequestStatus.DRAFT)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiếu yêu cầu {existingRequest.RequestCode} đang ở trạng thái '{existingRequest.Status}', chỉ có thể chỉnh sửa khi ở trạng thái Bản nháp (DRAFT).");
        }

        var updatedRows = await _context.BloodRequests
            .Where(r => r.RequestId == id && r.DoctorId == doctorId && r.Status == RequestStatus.DRAFT)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.PatientCode, patientCode)
                .SetProperty(r => r.PatientName, patientName)
                .SetProperty(r => r.PatientBloodType, request.PatientBloodType)
                .SetProperty(r => r.PatientRh, request.PatientRh)
                .SetProperty(r => r.UrgencyLevel, request.UrgencyLevel)
                .SetProperty(r => r.Diagnosis, diagnosisTrimmed));

        if (updatedRows == 0)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiếu yêu cầu {existingRequest.RequestCode} vừa được gửi hoặc không còn ở trạng thái Bản nháp (DRAFT).");
        }

        await _context.BloodRequestItems
            .Where(i => i.RequestId == id)
            .ExecuteDeleteAsync();

        foreach (var item in request.Items)
        {
            _context.BloodRequestItems.Add(new BloodRequestItem
            {
                RequestId = id,
                ComponentTypeId = item.ComponentTypeId,
                RequestedUnits = item.RequestedUnits,
                AllocatedUnits = 0
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            TableName = "blood_requests",
            RecordId = id,
            ActionType = ActionType.UPDATE,
            OldState = JsonSerializer.Serialize(new
            {
                patientCode = existingRequest.PatientCode,
                patientName = existingRequest.PatientName,
                patientBloodType = existingRequest.PatientBloodType.ToString(),
                patientRh = existingRequest.PatientRh.ToString(),
                urgencyLevel = existingRequest.UrgencyLevel.ToString(),
                diagnosis = existingRequest.Diagnosis,
                status = existingRequest.Status.ToString(),
                items = existingRequest.Items.Select(i => new { i.ComponentTypeId, i.RequestedUnits }).ToList()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                patientCode,
                patientName,
                patientBloodType = request.PatientBloodType.ToString(),
                patientRh = request.PatientRh.ToString(),
                urgencyLevel = request.UrgencyLevel.ToString(),
                diagnosis = diagnosisTrimmed,
                status = RequestStatus.DRAFT.ToString(),
                items = request.Items.Select(i => new { i.ComponentTypeId, i.RequestedUnits }).ToList()
            }),
            PerformedBy = doctorId,
            IpAddress = ipAddress,
            ChangeReason = $"Cập nhật nội dung phiếu nháp {existingRequest.RequestCode}.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetBloodRequestByIdAsync(id, doctorId);
    }

    public async Task<SubmitBloodRequestResponse> SubmitDraftBloodRequestAsync(int id, int doctorId, string? ipAddress)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu máu không hợp lệ.");
        }

        await BloodRequestProcessing.EnsureActiveUserAsync(_context, doctorId, "Thông tin bác sĩ đăng nhập không hợp lệ.");

        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var existingRequest = await _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (existingRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{id}.");
        }

        if (existingRequest.DoctorId != doctorId)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "Bạn chỉ có quyền gửi phiếu yêu cầu máu do chính mình tạo.");
        }

        if (existingRequest.Status != RequestStatus.DRAFT)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiếu yêu cầu {existingRequest.RequestCode} đang ở trạng thái '{existingRequest.Status}', chỉ có thể gửi phiếu đang ở trạng thái Bản nháp (DRAFT).");
        }

        if (existingRequest.Items.Count == 0 || existingRequest.Items.Any(i => i.RequestedUnits <= 0))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu nháp {existingRequest.RequestCode} chưa có chế phẩm máu hợp lệ để gửi.");
        }

        var updatedRows = await _context.BloodRequests
            .Where(r => r.RequestId == id && r.DoctorId == doctorId && r.Status == RequestStatus.DRAFT)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, RequestStatus.PENDING));

        if (updatedRows == 0)
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Phiếu yêu cầu {existingRequest.RequestCode} đã được gửi trước đó hoặc không còn ở trạng thái Bản nháp (DRAFT).");
        }

        _context.AuditLogs.Add(new AuditLog
        {
            TableName = "blood_requests",
            RecordId = id,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                requestId = existingRequest.RequestId,
                requestCode = existingRequest.RequestCode,
                status = RequestStatus.DRAFT.ToString()
            }),
            NewState = JsonSerializer.Serialize(new
            {
                requestId = existingRequest.RequestId,
                requestCode = existingRequest.RequestCode,
                status = RequestStatus.PENDING.ToString()
            }),
            PerformedBy = doctorId,
            IpAddress = ipAddress,
            ChangeReason = $"Gửi chính thức phiếu yêu cầu máu {existingRequest.RequestCode} từ bản nháp.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SubmitBloodRequestResponse
        {
            RequestId = existingRequest.RequestId,
            RequestCode = existingRequest.RequestCode,
            Status = RequestStatus.PENDING,
            CurrentStep = BloodRequestMappings.ResolveCurrentStep(RequestStatus.PENDING),
            SubmittedAt = now
        };
    }

    public async Task<PagedResult<ClinicalBloodRequestDto>> GetClinicalBloodRequestsAsync(ClinicalBloodRequestFilterDto filter, int doctorId)
    {
        if (doctorId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Thông tin bác sĩ đăng nhập không hợp lệ.");
        }

        BloodRequestProcessing.ValidatePagination(filter.PageNumber, filter.PageSize);

        var query = _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.DoctorId == doctorId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var kw = filter.Search.Trim();
            query = query.Where(r =>
                EF.Functions.ILike(r.RequestCode, $"%{kw}%") ||
                EF.Functions.ILike(r.PatientCode, $"%{kw}%") ||
                EF.Functions.ILike(r.PatientName, $"%{kw}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.UrgencyLevel))
        {
            if (!Enum.TryParse<UrgencyLevel>(filter.UrgencyLevel.Trim(), true, out var parsedUrgency))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Mức độ khẩn cấp '{filter.UrgencyLevel}' không hợp lệ.");
            }
            query = query.Where(r => r.UrgencyLevel == parsedUrgency);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (!Enum.TryParse<RequestStatus>(filter.Status.Trim(), true, out var parsedStatus))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Trạng thái phiếu '{filter.Status}' không hợp lệ.");
            }
            query = query.Where(r => r.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.RequestId)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ClinicalBloodRequestDto>
        {
            Items = records.Select(r => r.ToClinicalDto()).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<BloodRequestDetailDto> GetBloodRequestByIdAsync(int id, int currentUserId)
    {
        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu máu không hợp lệ.");
        }

        var bloodRequest = await _context.BloodRequests
            .AsNoTracking()
            .Include(r => r.Doctor)
                .ThenInclude(d => d!.Department)
            .Include(r => r.Items)
                .ThenInclude(i => i.ComponentType)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.Bag)
                    .ThenInclude(b => b!.ComponentType)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.ReservedByUser)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.IssuedByUser)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.ReceivedByUser)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (bloodRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{id}.");
        }

        if (bloodRequest.DoctorId != currentUserId)
        {
            var canViewAll = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == currentUserId && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .Where(ur => ur.Role != null && ur.Role.IsActive)
                .AnyAsync(ur =>
                    ur.Role!.RoleCode == "SYS" ||
                    ur.Role.RoleCode == "MGT" ||
                    ur.Role.RoleCode == "BBNK" ||
                    ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.PermissionCode == "BLOOD_REQUEST_APPROVE"));

            if (!canViewAll)
            {
                throw new HttpException(StatusCodes.Status403Forbidden, "Bạn chỉ có quyền xem chi tiết phiếu yêu cầu máu do chính mình tạo.");
            }
        }

        return bloodRequest.ToDetailDto();
    }

    public async Task<RejectBloodRequestResponse> RejectBloodRequestAsync(int id, RejectBloodRequestDto request, int userId, string? ipAddress)
    {
        await BloodRequestProcessing.EnsureActiveUserAsync(_context, userId, "Thông tin người từ chối không hợp lệ.");

        if (id <= 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã phiếu yêu cầu máu không hợp lệ.");
        }

        var reason = request.RejectionReason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Lý do từ chối (rejectionReason) không được để trống.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var bloodRequest = await _context.BloodRequests
            .Include(r => r.Items)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.Bag)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (bloodRequest == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, $"Không tìm thấy phiếu yêu cầu máu với mã #{id}.");
        }

        if (bloodRequest.Status != RequestStatus.PENDING)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, $"Phiếu yêu cầu {bloodRequest.RequestCode} đang ở trạng thái '{bloodRequest.Status}', chỉ có thể từ chối phiếu đang chờ xử lý (PENDING).");
        }

        var oldStatus = bloodRequest.Status;
        var now = DateTime.UtcNow;

        var reservedAllocations = bloodRequest.Allocations
            .Where(a => a.Status == AllocationStatus.RESERVED)
            .ToList();

        var releasedBags = new List<BloodBag>();
        foreach (var allocation in reservedAllocations)
        {
            if (allocation.Bag != null && allocation.Bag.Status == BagStatus.RESERVED)
            {
                allocation.Bag.Status = BagStatus.AVAILABLE;
                releasedBags.Add(allocation.Bag);
            }
        }

        if (reservedAllocations.Count > 0)
        {
            _context.BloodAllocations.RemoveRange(reservedAllocations);
        }

        foreach (var item in bloodRequest.Items)
        {
            item.AllocatedUnits = 0;
        }

        bloodRequest.Status = RequestStatus.REJECTED;
        bloodRequest.RejectionReason = reason;

        var auditLog = new AuditLog
        {
            TableName = "blood_requests",
            RecordId = bloodRequest.RequestId,
            ActionType = ActionType.STATUS_CHANGE,
            OldState = JsonSerializer.Serialize(new
            {
                status = oldStatus.ToString(),
                reservedAllocationCount = reservedAllocations.Count
            }),
            NewState = JsonSerializer.Serialize(new
            {
                status = bloodRequest.Status.ToString(),
                rejectionReason = bloodRequest.RejectionReason,
                releasedBagIds = releasedBags.Select(b => b.BagId).ToList()
            }),
            PerformedBy = userId,
            IpAddress = ipAddress,
            ChangeReason = $"Từ chối phiếu yêu cầu máu {bloodRequest.RequestCode}. Lý do: {reason}. Hoàn trả {releasedBags.Count} túi máu đã giữ chỗ.",
            CreatedAt = now
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        foreach (var bag in releasedBags)
        {
            _context.BloodBagStatusLogs.Add(new BloodBagStatusLog
            {
                BagId = bag.BagId,
                FromStatus = BagStatus.RESERVED,
                ToStatus = BagStatus.AVAILABLE,
                PerformedById = userId,
                AuditLogId = auditLog.LogId,
                Notes = $"Hoàn trả giữ chỗ do từ chối phiếu {bloodRequest.RequestCode}",
                CreatedAt = now
            });
        }

        if (releasedBags.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return new RejectBloodRequestResponse
        {
            RequestId = bloodRequest.RequestId,
            RequestCode = bloodRequest.RequestCode,
            Status = bloodRequest.Status,
            CurrentStep = BloodRequestMappings.ResolveCurrentStep(bloodRequest.Status),
            RejectionReason = bloodRequest.RejectionReason,
            ReleasedBagCount = releasedBags.Count,
            RejectedAt = now
        };
    }
}
