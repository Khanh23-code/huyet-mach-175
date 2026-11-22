using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Auth.DTOs.Auth;
using HuyetMach175.Modules.Auth.DTOs.Common;
using HuyetMach175.Modules.Auth.DTOs.Users;
using HuyetMach175.SharedKernel.Exceptions;
using HuyetMach175.SharedKernel.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Auth.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator, 
        IRefreshTokenGenerator refreshTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var identifier = !string.IsNullOrWhiteSpace(request.StaffCode)
            ? request.StaffCode.Trim()
            : request.Username?.Trim();

        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Vui lòng nhập tên đăng nhập hoặc mã nhân viên.");
        }

        var normalizedIdentifier = identifier.ToLower();

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedIdentifier || u.StaffCode.ToLower() == normalizedIdentifier);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "Tài khoản của bạn đã bị khóa hoặc vô hiệu hóa.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var roleCodes = user.UserRoles.Select(ur => ur.Role!.RoleCode.ToString()).ToList();

        var (accessToken, expireAt) = _jwtTokenGenerator.GenerateToken(
            user.UserId, 
            user.Username, 
            user.FullName, 
            user.DepartmentId, 
            roleCodes);

        string refreshToken = _refreshTokenGenerator.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpireAt = expireAt,
            UserInfo = await GetCurrentUserAsync(user.UserId)
        };
    }

    public async Task<bool> LogoutAsync(LogoutRequest request)
    {
        if (request == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Refresh Token không được để trống.");
        }

        var tokenEntity = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (tokenEntity != null && !tokenEntity.IsRevoked)
        {
            tokenEntity.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(LogoutRequest request)
    {
        if (request == null)
        {
            throw new HttpException(StatusCodes.Status400BadRequest, "Refresh Token không được để trống.");
        }

        var tokenEntity = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Refresh Token không hợp lệ hoặc đã hết hạn.");
        }

        var user = tokenEntity.User;

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Tài khoản người dùng không tồn tại.");
        }

        if (!user.IsActive)
        {
            throw new HttpException(StatusCodes.Status403Forbidden, "Tài khoản người dùng đã bị khóa hoặc vô hiệu hóa.");
        }

        var roleCodes = user.UserRoles
            .Select(ur => ur.Role!.RoleCode.ToString()).ToList();

        tokenEntity.IsRevoked = true;
        var (newAccesToken, newExpiredAt) = _jwtTokenGenerator.GenerateToken(
            tokenEntity.UserId,
            user.Username,
            user.FullName,
            user.DepartmentId,
            roleCodes);

        var newRefreshToken = _refreshTokenGenerator.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return new RefreshTokenResponse
        {
            NewAccessToken = newAccesToken,
            NewRefreshToken = newRefreshToken,
            NewExpiredAt = newExpiredAt,
        };
    }

    public async Task<UserInfoDto> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || !user.IsActive)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy thông tin người dùng hoặc tài khoản đã bị vô hiệu hóa.");
        }

        var activeRoles = user.UserRoles
            .Where(ur => ur.Role != null && ur.Role.IsActive)
            .Select(ur => ur.Role!)
            .ToList();

        var roleCodes = activeRoles
            .Select(r => r.RoleCode)
            .Distinct()
            .ToList();

        var permissions = activeRoles
            .SelectMany(r => r.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.PermissionCode)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        return new UserInfoDto
        {
            UserId = user.UserId,
            StaffCode = user.StaffCode,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.DepartmentName ?? string.Empty,
            Roles = roleCodes,
            Permissions = permissions
        };
    }

    public async Task<UserInfoDto> UpdateCurrentUserProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy thông tin người dùng trong hệ thống.");
        }

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        await _context.SaveChangesAsync();

        return await GetCurrentUserAsync(userId);
    }
}