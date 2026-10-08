using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Modules.Auth.DTOs;
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
            StaffCode = user.StaffCode,
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

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(UserFilterDto filter)
    {
        var query = _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
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
            query = query.Where(u => u.DepartmentId ==  filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.RoleCode))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role!.RoleCode.ToString() == filter.RoleCode));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.StaffCode)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserListItemDto
            {
                UserId = u.UserId,
                StaffCode = u.StaffCode,
                UserName = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department.DepartmentName,
                Roles = u.UserRoles.Select(ur => ur.Role!.RoleCode.ToString()).ToList()
            })
            .ToListAsync();

        return new PagedResult<UserListItemDto>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }
}