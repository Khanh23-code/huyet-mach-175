using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Auth.DTOs.Permissions;
using HuyetMach175.Modules.Auth.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Auth.Services;

public class PermissionsService : IPermissionsService
{
    private readonly ApplicationDbContext _context;

    public PermissionsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.ModuleCode)
            .ThenBy(p => p.PermissionId)
            .Select(PermissionMappings.ProjectToDto)
            .ToListAsync();

        return permissions;
    }

    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request)
    {
        if (request == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Dữ liệu gửi lên không hợp lệ.");
        }

        string moduleCode = request.ModuleCode.Trim().ToUpperInvariant();

        if (!Enum.TryParse<PermissionAction>(request.ActionType.Trim(), true, out var action) ||
            !Enum.IsDefined(typeof(PermissionAction), action))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Thao tác không hợp lệ.",
                new Dictionary<string, string> { ["actionType"] = "Thao tác phải là VIEW, EDIT, APPROVE hoặc CANCEL" });
        }

        string permissionCode = $"{moduleCode}_{action.ToString().ToUpperInvariant()}";

        if (await _context.Permissions.AnyAsync(p => p.PermissionCode == permissionCode))
        {
            throw new HttpException(StatusCodes.Status409Conflict, $"Quyền '{permissionCode}' đã tồn tại.",
                new Dictionary<string, string> { ["actionType"] = "Module này đã có thao tác đó" });
        }

        var existingModule = await _context.Permissions
            .FirstOrDefaultAsync(p => p.ModuleCode == moduleCode);

        string moduleName;
        if (existingModule != null)
        {
            moduleName = existingModule.ModuleName;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.ModuleName))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Module mới cần có tên hiển thị.",
                    new Dictionary<string, string> { ["moduleName"] = "Module mới cần có tên hiển thị" });
            }
            moduleName = request.ModuleName.Trim();
        }

        var permission = new Permission
        {
            ModuleCode = moduleCode,
            ModuleName = moduleName,
            ActionType = action,
            PermissionCode = permissionCode,
            PermissionName = request.PermissionName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        return PermissionMappings.ToDto(permission);
    }

    public async Task<CreatePermissionsResultDto> CreatePermissionsAsync(CreatePermissionsRequest request)
    {
        if (request == null || request.Actions == null || request.Actions.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng chọn ít nhất một thao tác.",
                new Dictionary<string, string> { ["actionTypes"] = "Chọn ít nhất 1 thao tác" });
        }

        string moduleCode = request.ModuleCode.Trim().ToUpperInvariant();

        // 1) Parse + kiểm tra từng thao tác, chặn trùng ngay trong request
        var parsed = new List<(PermissionAction Action, string Name)>();
        var seen = new HashSet<PermissionAction>();
        foreach (var item in request.Actions)
        {
            if (!Enum.TryParse<PermissionAction>(item.ActionType?.Trim(), true, out var action) ||
                !Enum.IsDefined(typeof(PermissionAction), action))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Thao tác không hợp lệ.",
                    new Dictionary<string, string> { ["actionTypes"] = "Thao tác phải là VIEW, EDIT, APPROVE hoặc CANCEL" });
            }

            if (!seen.Add(action))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, $"Thao tác '{action}' bị lặp trong yêu cầu.",
                    new Dictionary<string, string> { ["actionTypes"] = "Thao tác bị lặp" });
            }

            parsed.Add((action, item.PermissionName.Trim()));
        }

        // 2) Thao tác nào đã tồn tại thì bỏ qua
        var codes = parsed.Select(p => $"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}").ToList();
        var existingCodes = await _context.Permissions
            .Where(p => codes.Contains(p.PermissionCode))
            .Select(p => p.PermissionCode)
            .ToListAsync();
        var existingSet = existingCodes.ToHashSet();

        var toCreate = parsed
            .Where(p => !existingSet.Contains($"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}"))
            .ToList();

        var skipped = parsed
            .Where(p => existingSet.Contains($"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}"))
            .Select(p => p.Action.ToString().ToUpperInvariant())
            .ToList();

        if (toCreate.Count == 0)
        {
            throw new HttpException(StatusCodes.Status409Conflict, "Tất cả thao tác được chọn đã tồn tại trong phân hệ này.",
                new Dictionary<string, string> { ["actionTypes"] = "Phân hệ này đã có các thao tác đó" });
        }

        // 3) Tên module: module cũ kế thừa, module mới bắt buộc nhập
        var existingModule = await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ModuleCode == moduleCode);

        string moduleName;
        if (existingModule != null)
        {
            moduleName = existingModule.ModuleName;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.ModuleName))
            {
                throw new HttpException(StatusCodes.Status400BadRequest, "Module mới cần có tên hiển thị.",
                    new Dictionary<string, string> { ["moduleName"] = "Module mới cần có tên hiển thị" });
            }
            moduleName = request.ModuleName.Trim();
        }

        string? description = string.IsNullOrWhiteSpace(request.Description)
            ? existingModule?.Description
            : request.Description.Trim();

        var entities = toCreate.Select(p => new Permission
        {
            ModuleCode = moduleCode,
            ModuleName = moduleName,
            ActionType = p.Action,
            PermissionCode = $"{moduleCode}_{p.Action.ToString().ToUpperInvariant()}",
            PermissionName = p.Name,
            Description = description
        }).ToList();

        // 4) Một SaveChanges duy nhất = một transaction: hoặc tạo hết, hoặc không tạo gì
        _context.Permissions.AddRange(entities);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new HttpException(StatusCodes.Status409Conflict, "Có quyền vừa được tạo bởi người khác. Vui lòng tải lại và thử lại.");
        }

        return new CreatePermissionsResultDto
        {
            Created = entities.Select(p => PermissionMappings.ToDto(p)).ToList(),
            Skipped = skipped
        };
    }
}