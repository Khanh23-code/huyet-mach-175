using System.Security.Claims;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;

namespace HuyetMach175.SharedKernel.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            throw new HttpException(StatusCodes.Status401Unauthorized, "Không tìm thấy thông tin định danh người dùng trong Token.");
        }
        return userId;
    }
}