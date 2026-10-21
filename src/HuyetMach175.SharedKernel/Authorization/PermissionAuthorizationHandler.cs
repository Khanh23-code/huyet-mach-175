using System.Security.Claims;
using HuyetMach175.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.SharedKernel.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _context;

    public PermissionAuthorizationHandler(ApplicationDbContext context)
    {
        _context = context;
    }

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

        var hasPermission = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.IsActive)
            .AnyAsync(u => u.UserRoles.Any(ur =>
                ur.Role != null &&
                ur.Role.IsActive &&
                ur.Role.RolePermissions.Any(rp =>
                    rp.Permission != null &&
                    rp.Permission.PermissionCode == requirement.Permission)));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}