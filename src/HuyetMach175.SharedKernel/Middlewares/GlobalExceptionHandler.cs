using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuyetMach175.SharedKernel.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        string message;
        IDictionary<string, string>? errors = null;

        if (exception is HttpException httpEx)
        {
            statusCode = httpEx.StatusCode;
            message = httpEx.Message;
            errors = httpEx.Errors;
            _logger.LogWarning("HTTP {Status}: {Message}", statusCode, message);
        }
        else if (exception is DbUpdateException dbEx && IsUniqueViolation(dbEx))
        {
            statusCode = StatusCodes.Status409Conflict;
            message = "Dữ liệu đã tồn tại trong hệ thống (trùng tên đăng nhập, mã nhân viên, email hoặc số điện thoại).";
            _logger.LogWarning(exception, "Unique constraint violation");
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            message = "Đã xảy ra lỗi hệ thống.";
            _logger.LogError(exception, "An error occurred: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = new
        {
            statusCode,
            message,
            errors,
            timestamp = DateTime.UtcNow
        };

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null) return false;
        var sqlState = inner.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
        return sqlState == "23505";
    }
}