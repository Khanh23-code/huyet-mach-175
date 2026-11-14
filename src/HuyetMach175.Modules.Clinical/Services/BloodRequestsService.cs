using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.Modules.Clinical.DTOs.Common;
using HuyetMach175.Modules.Clinical.Mappings;
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
        if (doctorId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Thông tin bác sĩ đăng nhập không hợp lệ.");
        }

        var doctor = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == doctorId && u.IsActive);

        if (doctor == null)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Tài khoản bác sĩ không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        var patientCode = request.PatientCode?.Trim() ?? string.Empty;
        var patientName = request.PatientName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(patientCode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã người bệnh (patientCode) không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(patientName))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Tên người bệnh (patientName) không được để trống.");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiếu yêu cầu máu phải có ít nhất 1 loại chế phẩm.");
        }

        var duplicateComponentIds = request.Items
            .GroupBy(i => i.ComponentTypeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateComponentIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Không được chọn trùng loại chế phẩm máu trong cùng một phiếu yêu cầu.");
        }

        if (request.Items.Any(i => i.RequestedUnits <= 0))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số lượng đơn vị yêu cầu cho mỗi chế phẩm phải lớn hơn 0.");
        }

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
        var localDate = now.AddHours(7);
        var prefix = $"YC-{localDate:yyMMdd}-";

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var countToday = await _context.BloodRequests
            .CountAsync(r => r.RequestCode.StartsWith(prefix));
        var seq = countToday + 1;
        var requestCode = $"{prefix}{seq:D3}";

        while (await _context.BloodRequests.AnyAsync(r => r.RequestCode == requestCode))
        {
            seq++;
            requestCode = $"{prefix}{seq:D3}";
        }

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

    public async Task<PagedResult<ClinicalBloodRequestDto>> GetClinicalBloodRequestsAsync(ClinicalBloodRequestFilterDto filter, int doctorId)
    {
        if (doctorId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Thông tin bác sĩ đăng nhập không hợp lệ.");
        }

        if (filter.PageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }

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
}
