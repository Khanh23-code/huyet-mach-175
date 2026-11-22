using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HuyetMach175.Modules.Inventory.BackgroundServices;

public class BloodBagExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BloodBagExpirationWorker> _logger;

    public BloodBagExpirationWorker(IServiceProvider serviceProvider, ILogger<BloodBagExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BloodBagExpirationWorker is starting.");

        await CheckAndLockExpiredBagsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate delay until next 00:00:00 (Vietnam Time UTC+7)
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetVietnamTimeZone());
            var tomorrowMidnightVn = nowVn.Date.AddDays(1);
            var delay = tomorrowMidnightVn - nowVn;

            if (delay <= TimeSpan.Zero)
            {
                delay = TimeSpan.FromMinutes(1);
            }

            _logger.LogInformation("BloodBagExpirationWorker scheduled next run in {DelayHours:0.##} hours at {NextRun}.", delay.TotalHours, tomorrowMidnightVn);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await CheckAndLockExpiredBagsAsync();
        }

        _logger.LogInformation("BloodBagExpirationWorker is stopping.");
    }

    public async Task<int> CheckAndLockExpiredBagsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;

            var expiredBags = await context.BloodBags
                .Where(b => b.ExpiredAt <= now &&
                            b.Status != BagStatus.LOCKED &&
                            b.Status != BagStatus.DISCARDED &&
                            b.Status != BagStatus.TRANSFUSED &&
                            b.Status != BagStatus.PROCESSED)
                .ToListAsync();

            if (expiredBags.Count == 0)
            {
                return 0;
            }

            _logger.LogInformation("Found {Count} expired blood bag(s) to automatically lock.", expiredBags.Count);

            foreach (var bag in expiredBags)
            {
                var oldStatus = bag.Status;
                bag.Status = BagStatus.LOCKED;
                bag.DiscardReason = "EXPIRED";

                var auditLog = new AuditLog
                {
                    TableName = "blood_bags",
                    RecordId = bag.BagId,
                    ActionType = ActionType.STATUS_CHANGE,
                    OldState = JsonSerializer.Serialize(new { status = oldStatus.ToString(), expiredAt = bag.ExpiredAt }),
                    NewState = JsonSerializer.Serialize(new { status = BagStatus.LOCKED.ToString(), discardReason = "EXPIRED" }),
                    PerformedBy = null,
                    IpAddress = "127.0.0.1 (System Worker)",
                    ChangeReason = $"Hệ thống tự động tạm khóa do hết hạn sử dụng vào lúc {bag.ExpiredAt:dd/MM/yyyy HH:mm}.",
                    CreatedAt = now
                };
                context.AuditLogs.Add(auditLog);
                await context.SaveChangesAsync();

                var statusLog = new BloodBagStatusLog
                {
                    BagId = bag.BagId,
                    FromStatus = oldStatus,
                    ToStatus = BagStatus.LOCKED,
                    PerformedById = null,
                    AuditLogId = auditLog.LogId,
                    Notes = "Hệ thống tự động tạm khóa do hết hạn sử dụng lúc 00:00",
                    CreatedAt = now
                };
                context.BloodBagStatusLogs.Add(statusLog);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Successfully locked {Count} expired blood bag(s).", expiredBags.Count);
            return expiredBags.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking and locking expired blood bags.");
            return 0;
        }
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux/macOS
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "Vietnam Standard Time", "Vietnam Standard Time");
            }
        }
    }
}