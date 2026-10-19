using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Auth.DTOs.Auth;
using HuyetMach175.Modules.Auth.DTOs.Common;
using HuyetMach175.Modules.Auth.DTOs.Users;
using HuyetMach175.SharedKernel.Exceptions;
using HuyetMach175.SharedKernel.Services;
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
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            throw new HttpException(400, "Username or password was not correct.");
        }

        if (!user.IsActive)
        {
            throw new HttpException(403, "Your account had been blocked.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new HttpException(400, "Username or password was not correct.");
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
            throw new HttpException(400, "Refresh Token must not be empty.");
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
            throw new HttpException(400, "Refresh Token must not be empty.");
        }

        var tokenEntity = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            throw new HttpException(401, "Refresh Token is invalid or expired.");
        }

        var user = tokenEntity.User;

        if (user == null)
        {
            throw new HttpException(404, "This account is not exist.");
        }

        if (!user.IsActive)
        {
            throw new HttpException(403, "This user had been blocked.");
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
            throw new HttpException(404, "Cannot find user information.");
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
            throw new HttpException(404, "This user was not found in the system.");
        }

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        await _context.SaveChangesAsync();

        return await GetCurrentUserAsync(userId);
    }
}