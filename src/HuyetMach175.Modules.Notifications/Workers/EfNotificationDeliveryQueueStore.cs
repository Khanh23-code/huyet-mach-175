using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Notifications.Workers;

public sealed record ClaimedNotificationDelivery(
    Guid DeliveryId,
    Guid? NotificationId,
    string Destination,
    int AttemptCount,
    string? PayloadJson);

public sealed record NotificationDeliveryOutcome(
    Guid DeliveryId,
    NotificationDeliveryStatus Status,
    int AttemptCount,
    DateTime NextAttemptAtUtc,
    DateTime? SentAtUtc = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface INotificationDeliveryQueueStore
{
    Task<IReadOnlyList<ClaimedNotificationDelivery>> ClaimBatchAsync(
        int batchSize,
        int staleProcessingMinutes,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task CompleteBatchAsync(
        IReadOnlyCollection<NotificationDeliveryOutcome> outcomes,
        CancellationToken cancellationToken = default);
}

public sealed class EfNotificationDeliveryQueueStore : INotificationDeliveryQueueStore
{
    private readonly ApplicationDbContext _context;

    public EfNotificationDeliveryQueueStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClaimedNotificationDelivery>> ClaimBatchAsync(
        int batchSize,
        int staleProcessingMinutes,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var safeBatchSize = Math.Clamp(batchSize, 1, 100);
        var staleCutoffUtc = nowUtc.AddMinutes(-Math.Max(1, staleProcessingMinutes));
        var leaseExpiresAtUtc = nowUtc.AddMinutes(Math.Max(1, staleProcessingMinutes));

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var rows = await _context.NotificationDeliveries
            .FromSqlInterpolated($"""
                SELECT *
                FROM notification_delivery
                WHERE channel = 'Email'
                  AND ((status IN ('Pending', 'RetryScheduled') AND next_attempt_at <= {nowUtc})
                    OR (status = 'Processing' AND next_attempt_at <= {staleCutoffUtc}))
                ORDER BY next_attempt_at, delivery_id
                LIMIT {safeBatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return [];
        }

        foreach (var row in rows)
        {
            row.Status = NotificationDeliveryStatus.Processing;
            row.NextAttemptAt = leaseExpiresAtUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return rows
            .Select(r => new ClaimedNotificationDelivery(
                r.DeliveryId,
                r.NotificationId,
                r.Destination,
                r.AttemptCount,
                r.PayloadJson))
            .ToList();
    }

    public async Task CompleteBatchAsync(
        IReadOnlyCollection<NotificationDeliveryOutcome> outcomes,
        CancellationToken cancellationToken = default)
    {
        if (outcomes.Count == 0)
        {
            return;
        }

        var outcomeById = outcomes.ToDictionary(o => o.DeliveryId);
        var ids = outcomeById.Keys.ToList();

        var tracked = await _context.NotificationDeliveries
            .Where(d => ids.Contains(d.DeliveryId))
            .ToListAsync(cancellationToken);

        foreach (var entity in tracked)
        {
            if (!outcomeById.TryGetValue(entity.DeliveryId, out var outcome))
            {
                continue;
            }

            entity.Status = outcome.Status;
            entity.AttemptCount = outcome.AttemptCount;
            entity.NextAttemptAt = outcome.NextAttemptAtUtc;
            entity.SentAt = outcome.SentAtUtc ?? entity.SentAt;
            entity.LastErrorCode = outcome.ErrorCode;
            entity.LastErrorMessage = outcome.ErrorMessage;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
