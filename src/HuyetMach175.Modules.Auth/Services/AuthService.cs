using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs;
using HuyetMach175.SharedKernel.Services;
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
            throw new Exception("Username or password was not correct.");
        }

        if (!user.IsActive)
        {
            throw new Exception("Your account had been blocked.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new Exception("Username or password was not correct.");
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
            UserInfo = await GetUserInfoAsync(user.UserId)
        };
    }

    public async Task<UserInfoDto> GetUserInfoAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || !user.IsActive)
        {
            throw new Exception("Cannot find user information.");
        }

        var roleCodes = user.UserRoles
            .Select(ur => ur.Role!.RoleCode.ToString()).ToList();

        return new UserInfoDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            Roles = roleCodes
        };
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (request == null)
        {
            throw new Exception("Refresh Token must not be empty.");
        }

        var tokenEntity = await _context.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            throw new Exception("Refresh Token is invalid or expired.");
        }

        var user = tokenEntity.User;

        if (user == null)
        {
            throw new Exception("This account is not exist.");
        }

        if (!user.IsActive)
        {
            throw new Exception("This user had been blocked.");
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
}