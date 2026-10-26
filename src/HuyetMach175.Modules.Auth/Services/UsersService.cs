using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs.Common;
using HuyetMach175.Modules.Auth.DTOs.Users;
using HuyetMach175.Modules.Auth.Mappings;
using HuyetMach175.SharedKernel.Exceptions;
using HuyetMach175.SharedKernel.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Auth.Services;

public class UsersService : IUsersService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UsersService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(UserFilterDto filter)
    {
        var query = _context.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.KeyWord))
        {
            var kw = filter.KeyWord.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(kw) ||
                u.FullName.ToLower().Contains(kw) ||
                u.StaffCode.ToLower().Contains(kw) ||
                (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(u => u.DepartmentId == filter.DepartmentId.Value);
        }

        var targetRoles = new List<string>();
        if (filter.RoleCodes != null && filter.RoleCodes.Count > 0)
        {
            foreach (var rc in filter.RoleCodes)
            {
                if (!string.IsNullOrWhiteSpace(rc))
                {
                    var parts = rc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var part in parts)
                    {
                        if (!targetRoles.Contains(part, StringComparer.OrdinalIgnoreCase))
                            targetRoles.Add(part.ToUpperInvariant());
                    }
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(filter.RoleCode))
        {
            var parts = filter.RoleCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!targetRoles.Contains(part, StringComparer.OrdinalIgnoreCase))
                    targetRoles.Add(part.ToUpperInvariant());
            }
        }
        if (targetRoles.Count > 0)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && targetRoles.Contains(ur.Role.RoleCode)));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.StaffCode)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(UserMappings.ProjectToListItem)
            .ToListAsync();

        return new PagedResult<UserListItemDto>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<UserListItemDto> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(UserMappings.ProjectToListItem)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy người dùng trong hệ thống.");
        }

        return user;
    }
    
    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task EnsureUniqueAsync(
        string username, string staffCode, string? email, string? phone, int? excludeUserId = null)
    {
        var errors = new Dictionary<string, string>();

        var others = _context.Users.AsNoTracking()
            .Where(u => excludeUserId == null || u.UserId != excludeUserId);

        if (await others.AnyAsync(u => u.Username == username))
            errors["username"] = "Tên đăng nhập đã được sử dụng";

        if (await others.AnyAsync(u => u.StaffCode == staffCode))
            errors["staffCode"] = "Mã nhân viên đã tồn tại";

        if (email != null && await others.AnyAsync(u => u.Email != null && u.Email.ToLower() == email))
            errors["email"] = "Email đã được sử dụng";

        if (phone != null && await others.AnyAsync(u => u.PhoneNumber == phone))
            errors["phoneNumber"] = "Số điện thoại đã được sử dụng";

        if (errors.Count > 0)
            throw new HttpException(StatusCodes.Status409Conflict, "Dữ liệu bị trùng với người dùng khác", errors);
    }

    public async Task<UserListItemDto> CreateUserAsync(CreateUserRequest request, bool isSysAdmin = true)
    {
        if (request == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Dữ liệu gửi lên không hợp lệ.");
        }

        string username = request.Username.Trim();
        string staffCode = request.StaffCode.Trim().ToUpperInvariant();
        string? email = NormalizeOptional(request.Email)?.ToLowerInvariant();
        string? phone = NormalizeOptional(request.PhoneNumber);

        await EnsureUniqueAsync(username, staffCode, email, phone);

        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == request.DepartmentId))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Khoa/phòng không tồn tại.",
                new Dictionary<string, string> { ["departmentId"] = "Khoa/phòng không tồn tại" });
        }

        var requestedRoleIds = request.RoleIds.Distinct().ToList();
        var targetRoles = await _context.Roles
            .Where(r => requestedRoleIds.Contains(r.RoleId))
            .ToListAsync();

        if (targetRoles.Count != requestedRoleIds.Count)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Có vai trò không hợp lệ.",
                new Dictionary<string, string> { ["roleIds"] = "Có vai trò không hợp lệ" });
        }

        if (targetRoles.Any(r => !r.IsActive))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Không thể gán vai trò đã bị vô hiệu hóa.",
                new Dictionary<string, string> { ["roleIds"] = "Có vai trò đã bị vô hiệu hóa" });
        }

        if (!isSysAdmin && targetRoles.Any(r => r.RoleCode == "SYS"))
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "Chỉ quản trị hệ thống (SYS) mới được gán vai trò SYS.");
        }

        var user = new User
        {
            DepartmentId = request.DepartmentId,
            Username = username,
            StaffCode = staffCode,
            FullName = request.FullName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Email = email,
            PhoneNumber = phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var role in targetRoles)
        {
            user.UserRoles.Add(new UserRole
            {
                RoleId = role.RoleId,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(user.UserId);
    }

    public async Task<UserListItemDto> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy người dùng.");
        }

        if (!await _context.Departments.AnyAsync(d => d.DepartmentId == request.DepartmentId))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Khoa/phòng không tồn tại.",
                new Dictionary<string, string> { ["departmentId"] = "Khoa/phòng không tồn tại" });
        }

        string? email = NormalizeOptional(request.Email)?.ToLowerInvariant();
        string? phone = NormalizeOptional(request.PhoneNumber);

        await EnsureUniqueAsync(user.Username, user.StaffCode, email, phone, excludeUserId: userId);

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.PhoneNumber = phone;
        user.DepartmentId = request.DepartmentId;
        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(userId);
    }

    public async Task<UserListItemDto> UpdateUserStatusAsync(int userId, UpdateUserStatusRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Mã người dùng này không tồn tại.");
        }

        user.IsActive = request.IsActive;

        if (request.IsActive == false)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => !rt.IsRevoked && rt.UserId == userId)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
            }
        }

        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(userId);
    }

    public async Task<UserListItemDto> UpdateUserRolesAsync(int userId, UpdateUserRolesRequest request, bool isSysAdmin = true)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy người dùng trong hệ thống.");
        }

        if (!isSysAdmin && user.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleCode == "SYS"))
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "Chỉ quản trị hệ thống (SYS) mới có quyền chỉnh sửa vai trò của người dùng có vai trò SYS.");
        }

        if (request.UserRoles == null || request.UserRoles.Count == 0)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Người dùng phải có ít nhất một vai trò.");
        }

        var requestedRoleIds = request.UserRoles.Distinct().ToList();
        var targetRoles = await _context.Roles
            .Where(r => requestedRoleIds.Contains(r.RoleId))
            .ToListAsync();

        if (targetRoles.Count != requestedRoleIds.Count)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Một hoặc nhiều mã vai trò không hợp lệ.");
        }

        if (!isSysAdmin && targetRoles.Any(r => r.RoleCode == "SYS"))
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "Chỉ quản trị hệ thống (SYS) mới được phép gán vai trò SYS.");
        }

        var currentRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        _context.UserRoles.RemoveRange(currentRoles);

        foreach (var role in targetRoles)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = role.RoleId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(userId);
    }

    public async Task<bool> ResetPasswordAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy người dùng trong hệ thống.");
        }

        var activeToken = await _context.RefreshTokens.Where(rt => rt.UserId == userId && rt.IsRevoked == false).ToListAsync();

        foreach (var token in activeToken)
        {
            token.IsRevoked = true;
        }

        user.PasswordHash = _passwordHasher.HashPassword("password123");

        await _context.SaveChangesAsync();

        return true;
    }
}
