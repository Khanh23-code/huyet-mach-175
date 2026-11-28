using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Notifications.Email;
using HuyetMach175.Modules.Notifications.Options;
using HuyetMach175.SharedKernel.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HuyetMach175.Modules.Notifications.Workers;

public sealed class NotificationDeliveryWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailSender _emailSender;
    private readonly INotificationEmailTemplateRenderer _templateRenderer;
    private readonly IOptionsMonitor<NotificationDeliveryOptions> _deliveryOptions;
    private readonly IOptionsMonitor<SmtpOptions> _smtpOptions;
    private readonly ILogger<NotificationDeliveryWorker> _logger;

    private bool _loggedMissingSmtpNotice;

    public NotificationDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IEmailSender emailSender,
        INotificationEmailTemplateRenderer templateRenderer,
        IOptionsMonitor<NotificationDeliveryOptions> deliveryOptions,
        IOptionsMonitor<SmtpOptions> smtpOptions,
        ILogger<NotificationDeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _deliveryOptions = deliveryOptions;
        _smtpOptions = smtpOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var initialPollSeconds = Math.Max(2, _deliveryOptions.CurrentValue.PollIntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(initialPollSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ngoài dự kiến trong chu kỳ xử lý của NotificationDeliveryWorker.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        var deliveryOpts = _deliveryOptions.CurrentValue;
        if (!deliveryOpts.Enabled)
        {
            return;
        }

        if (!_smtpOptions.CurrentValue.IsConfigured)
        {
            if (!_loggedMissingSmtpNotice)
            {
                _logger.LogInformation(
                    "Chưa cấu hình Notifications:Smtp (Host/Password trống). Hộp thư In-App vẫn hoạt động bình thường; Worker gửi Gmail sẽ tự chạy khi bạn điền cấu hình SMTP.");
                _loggedMissingSmtpNotice = true;
            }
            return;
        }

        _loggedMissingSmtpNotice = false;

        var batchSize = Math.Clamp(deliveryOpts.BatchSize, 1, 100);
        var maxDrain = Math.Clamp(deliveryOpts.MaxDrainIterations, 1, 10);

        for (var iteration = 0; iteration < maxDrain; iteration++)
        {
            var processedCount = await DispatchNextBatchAsync(deliveryOpts, batchSize, cancellationToken);
            if (processedCount < batchSize)
            {
                break;
            }
        }
    }

    private async Task<int> DispatchNextBatchAsync(
        NotificationDeliveryOptions deliveryOpts,
        int batchSize,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ClaimedNotificationDelivery> claimed;
        using (var claimScope = _scopeFactory.CreateScope())
        {
            var queueStore = claimScope.ServiceProvider.GetRequiredService<INotificationDeliveryQueueStore>();
            claimed = await queueStore.ClaimBatchAsync(
                batchSize,
                deliveryOpts.StaleProcessingMinutes,
                DateTime.UtcNow,
                cancellationToken);
        }

        if (claimed.Count == 0)
        {
            return 0;
        }

        var outcomes = new List<NotificationDeliveryOutcome>(claimed.Count);
        var emailBatch = new List<EmailBatchItem>(claimed.Count);
        var claimedById = claimed.ToDictionary(c => c.DeliveryId);

        // Dự phòng cho các dòng không có sẵn payload_json nhưng có liên kết notification_id
        Dictionary<Guid, NotificationEmailPayload>? fallbackPayloads = null;
        var missingPayloadNotificationIds = claimed
            .Where(c => string.IsNullOrWhiteSpace(c.PayloadJson) && c.NotificationId.HasValue)
            .Select(c => c.NotificationId!.Value)
            .Distinct()
            .ToList();

        if (missingPayloadNotificationIds.Count > 0)
        {
            fallbackPayloads = await LoadFallbackPayloadsAsync(missingPayloadNotificationIds, cancellationToken);
        }

        var nowUtc = DateTime.UtcNow;

        foreach (var item in claimed)
        {
            if (deliveryOpts.ShouldSkipDestination(item.Destination))
            {
                outcomes.Add(new NotificationDeliveryOutcome(
                    item.DeliveryId,
                    NotificationDeliveryStatus.Skipped,
                    item.AttemptCount,
                    nowUtc,
                    ErrorCode: "SKIPPED_PLACEHOLDER_DOMAIN",
                    ErrorMessage: "Bỏ qua gửi SMTP cho tên miền nội bộ giả lập."));
                continue;
            }

            NotificationEmailPayload? payload = null;
            if (!string.IsNullOrWhiteSpace(item.PayloadJson))
            {
                try
                {
                    payload = JsonSerializer.Deserialize<NotificationEmailPayload>(item.PayloadJson, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Không đọc được payload_json của DeliveryId={DeliveryId}.", item.DeliveryId);
                    outcomes.Add(new NotificationDeliveryOutcome(
                        item.DeliveryId,
                        NotificationDeliveryStatus.FailedPermanent,
                        item.AttemptCount + 1,
                        nowUtc,
                        ErrorCode: "INVALID_PAYLOAD_JSON",
                        ErrorMessage: ex.Message));
                    continue;
                }
            }
            else if (item.NotificationId.HasValue && fallbackPayloads is not null)
            {
                fallbackPayloads.TryGetValue(item.NotificationId.Value, out payload);
            }

            if (payload is null)
            {
                outcomes.Add(new NotificationDeliveryOutcome(
                    item.DeliveryId,
                    NotificationDeliveryStatus.FailedPermanent,
                    item.AttemptCount + 1,
                    nowUtc,
                    ErrorCode: "MISSING_EMAIL_PAYLOAD",
                    ErrorMessage: "Không tìm thấy nội dung email để kết xuất."));
                continue;
            }

            var renderedMessage = _templateRenderer.Render(item.Destination, payload);
            emailBatch.Add(new EmailBatchItem(item.DeliveryId, renderedMessage));
        }

        if (emailBatch.Count > 0)
        {
            var sendResults = await _emailSender.SendBatchAsync(emailBatch, cancellationToken);
            var completedAtUtc = DateTime.UtcNow;
            var maxAttempts = Math.Max(1, deliveryOpts.MaxAttempts);

            foreach (var result in sendResults)
            {
                var source = claimedById[result.Id];
                var nextAttemptCount = source.AttemptCount + 1;

                if (result.Succeeded)
                {
                    outcomes.Add(new NotificationDeliveryOutcome(
                        result.Id,
                        NotificationDeliveryStatus.Accepted,
                        nextAttemptCount,
                        completedAtUtc,
                        SentAtUtc: completedAtUtc));
                    continue;
                }

                if (result.IsPermanentFailure || nextAttemptCount >= maxAttempts)
                {
                    _logger.LogWarning(
                        "Email DeliveryId={DeliveryId} tới '{Destination}' chuyển sang FailedPermanent sau {AttemptCount} lần thử ({ErrorCode}: {ErrorMessage}).",
                        result.Id,
                        source.Destination,
                        nextAttemptCount,
                        result.ErrorCode,
                        result.ErrorMessage);

                    outcomes.Add(new NotificationDeliveryOutcome(
                        result.Id,
                        NotificationDeliveryStatus.FailedPermanent,
                        nextAttemptCount,
                        completedAtUtc,
                        ErrorCode: result.ErrorCode,
                        ErrorMessage: result.ErrorMessage));
                    continue;
                }

                var retryDelay = CalculateBackoff(nextAttemptCount);
                outcomes.Add(new NotificationDeliveryOutcome(
                    result.Id,
                    NotificationDeliveryStatus.RetryScheduled,
                    nextAttemptCount,
                    completedAtUtc.Add(retryDelay),
                    ErrorCode: result.ErrorCode,
                    ErrorMessage: result.ErrorMessage));
            }
        }

        using (var completeScope = _scopeFactory.CreateScope())
        {
            var queueStore = completeScope.ServiceProvider.GetRequiredService<INotificationDeliveryQueueStore>();
            await queueStore.CompleteBatchAsync(outcomes, cancellationToken);
        }

        return claimed.Count;
    }

    private async Task<Dictionary<Guid, NotificationEmailPayload>> LoadFallbackPayloadsAsync(
        IReadOnlyCollection<Guid> notificationIds,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rows = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => notificationIds.Contains(n.NotificationId))
            .Select(n => new
            {
                n.NotificationId,
                n.Title,
                n.Content,
                n.Priority,
                n.ActionPath,
                RecipientFullName = n.RecipientUser != null ? n.RecipientUser.FullName : null
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.NotificationId,
            r => new NotificationEmailPayload(
                Subject: $"[Huyết Mạch 175] {r.Title}",
                Title: r.Title,
                Content: r.Content,
                RecipientFullName: r.RecipientFullName,
                Priority: r.Priority,
                ActionPath: r.ActionPath));
    }

    private static TimeSpan CalculateBackoff(int attemptCount) => attemptCount switch
    {
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        3 => TimeSpan.FromMinutes(15),
        4 => TimeSpan.FromMinutes(30),
        _ => TimeSpan.FromMinutes(60)
    };
}
