using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HuyetMach175.Modules.Clinical.BackgroundServices;

public class HandoverSessionExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HandoverSessionExpirationWorker> _logger;

    public HandoverSessionExpirationWorker(IServiceProvider serviceProvider, ILogger<HandoverSessionExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HandoverSessionExpirationWorker is starting.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunExpirationPassAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring overdue blood handover sessions.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("HandoverSessionExpirationWorker is stopping.");
    }

    private async Task RunExpirationPassAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var expiredCount = await ExpireOverdueSessionsAsync(context, DateTime.UtcNow, stoppingToken);

        if (expiredCount > 0)
        {
            _logger.LogInformation("Automatically expired {Count} overdue blood handover session(s).", expiredCount);
        }
    }

    public static async Task<int> ExpireOverdueSessionsAsync(
        ApplicationDbContext context,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var overdueSessions = await context.BloodHandoverSessions
            .Include(s => s.Items)
                .ThenInclude(i => i.Bag)
            .Where(s => s.Status == HandoverSessionStatus.PENDING && s.ExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

        if (overdueSessions.Count == 0)
        {
            return 0;
        }

        foreach (var session in overdueSessions)
        {
            session.Status = HandoverSessionStatus.EXPIRED;

            context.AuditLogs.Add(new AuditLog
            {
                TableName = "blood_handover_sessions",
                RecordId = session.HandoverSessionId,
                ActionType = ActionType.STATUS_CHANGE,
                OldState = JsonSerializer.Serialize(new
                {
                    status = HandoverSessionStatus.PENDING.ToString()
                }),
                NewState = JsonSerializer.Serialize(new
                {
                    status = HandoverSessionStatus.EXPIRED.ToString(),
                    expiredAt = nowUtc,
                    bagIds = session.Items.Select(i => i.BagId).ToList(),
                    barcodes = session.Items.Where(i => i.Bag != null).Select(i => i.Bag!.Barcode).ToList()
                }),
                PerformedBy = null,
                ChangeReason = $"Phiên bàn giao {session.SessionCode} tự động hết hạn (timeout); giữ nguyên trạng thái RESERVED và vị trí tủ cho các túi máu.",
                CreatedAt = nowUtc
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return overdueSessions.Count;
    }
}
