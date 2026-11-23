using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Clinical.DTOs.BloodRequests;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Clinical.Processing;

public static class BloodRequestProcessing
{
    public static async Task EnsureActiveUserAsync(ApplicationDbContext context, int userId, string errorMessage)
    {
        await LoadActiveUserAsync(context, userId, errorMessage);
    }

    public static async Task<User> LoadActiveUserAsync(ApplicationDbContext context, int userId, string errorMessage)
    {
        if (userId <= 0)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, errorMessage);
        }

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Tài khoản thực hiện không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        return user;
    }

    public static void EnsureSameDepartmentAccess(User currentUser, BloodRequest? bloodRequest, string errorMessage)
    {
        if (bloodRequest?.Doctor == null || bloodRequest.Doctor.DepartmentId != currentUser.DepartmentId)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, errorMessage);
        }
    }

    public static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số trang (pageNumber) phải lớn hơn hoặc bằng 1.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Kích thước trang (pageSize) phải nằm trong khoảng từ 1 đến 100.");
        }
    }

    public static (string PatientCode, string PatientName) ValidateCreateBloodRequest(CreateBloodRequestDto request)
        => ValidateBloodRequestPayload(request.PatientCode, request.PatientName, request.Items);

    public static (string PatientCode, string PatientName) ValidateUpdateBloodRequest(UpdateBloodRequestDto request)
        => ValidateBloodRequestPayload(request.PatientCode, request.PatientName, request.Items);

    private static (string PatientCode, string PatientName) ValidateBloodRequestPayload(
        string? rawPatientCode,
        string? rawPatientName,
        List<CreateBloodRequestItemDto>? items)
    {
        var patientCode = rawPatientCode?.Trim() ?? string.Empty;
        var patientName = rawPatientName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(patientCode))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Mã người bệnh (patientCode) không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(patientName))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Tên người bệnh (patientName) không được để trống.");
        }

        if (items == null || items.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Phiếu yêu cầu máu phải có ít nhất 1 loại chế phẩm.");
        }

        var duplicateComponentIds = items
            .GroupBy(i => i.ComponentTypeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateComponentIds.Count > 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Không được chọn trùng loại chế phẩm máu trong cùng một phiếu yêu cầu.");
        }

        if (items.Any(i => i.RequestedUnits <= 0))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Số lượng đơn vị yêu cầu cho mỗi chế phẩm phải lớn hơn 0.");
        }

        return (patientCode, patientName);
    }

    public static async Task<bool> HasHospitalWideClinicalAccessAsync(ApplicationDbContext context, int userId, string approvePermissionCode)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.IsActive)
            .SelectMany(u => u.UserRoles)
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .AnyAsync(ur =>
                ur.Role!.RoleCode == "SYS" ||
                ur.Role.RoleCode == "MGT" ||
                ur.Role.RoleCode == "BBNK" ||
                ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.PermissionCode == approvePermissionCode));
    }

    public static async Task<string> GenerateRequestCodeAsync(ApplicationDbContext context, DateTime nowUtc)
    {
        var localDate = nowUtc.AddHours(7);
        var prefix = $"YC-{localDate:yyMMdd}-";

        var countToday = await context.BloodRequests
            .CountAsync(r => r.RequestCode.StartsWith(prefix));
        var seq = countToday + 1;
        var requestCode = $"{prefix}{seq:D3}";

        while (await context.BloodRequests.AnyAsync(r => r.RequestCode == requestCode))
        {
            seq++;
            requestCode = $"{prefix}{seq:D3}";
        }

        return requestCode;
    }

    public static async Task<string> GenerateHandoverSessionCodeAsync(ApplicationDbContext context, DateTime nowUtc)
    {
        var localDate = nowUtc.AddHours(7);
        var prefix = $"BG-{localDate:yyMMdd}-";

        var countToday = await context.BloodHandoverSessions
            .CountAsync(s => s.SessionCode.StartsWith(prefix));
        var seq = countToday + 1;
        var sessionCode = $"{prefix}{seq:D3}";

        while (await context.BloodHandoverSessions.AnyAsync(s => s.SessionCode == sessionCode))
        {
            seq++;
            sessionCode = $"{prefix}{seq:D3}";
        }

        return sessionCode;
    }
}
