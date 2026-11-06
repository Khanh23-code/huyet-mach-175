using System.Security.Claims;
using HuyetMach175.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HuyetMach175.SharedKernel.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public PermissionAuthorizationHandler(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>Key cache quyền của user. Gọi _cache.Remove(CacheKey(userId)) khi đổi role/status user.</summary>
    public static string CacheKey(int userId) => $"perm:{userId}";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var codes = await _cache.GetOrCreateAsync(CacheKey(userId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

            var list = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .Where(ur => ur.Role != null && ur.Role.IsActive)
                .SelectMany(ur => ur.Role!.RolePermissions)
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.PermissionCode)
                .Distinct()
                .ToListAsync();

            return list.ToHashSet();
        });

        if (codes is not null && codes.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}