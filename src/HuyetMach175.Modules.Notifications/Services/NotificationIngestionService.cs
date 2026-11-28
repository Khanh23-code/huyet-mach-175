using System.Text.Json;
using HuyetMach175.Api.Data;
using HuyetMach175.Api.Data.Entities;
using HuyetMach175.Api.Data.Enums;
using HuyetMach175.Modules.Notifications.Email;
using HuyetMach175.Modules.Notifications.Options;
using HuyetMach175.SharedKernel.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HuyetMach175.Modules.Notifications.Services;

public sealed class NotificationIngestionService : INotificationIngestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly IOptionsMonitor<NotificationDeliveryOptions> _deliveryOptions;
    private readonly ILogger<NotificationIngestionService> _logger;

    public NotificationIngestionService(
        ApplicationDbContext context,
        IOptionsMonitor<NotificationDeliveryOptions> deliveryOptions,
        ILogger<NotificationIngestionService> logger)
    {
        _context = context;
        _deliveryOptions = deliveryOptions;
        _logger = logger;
    }

    public async Task<NotificationIngestionResult> StageAsync(
        NotificationIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RecipientUserId == Guid.Empty)
        {
            return new NotificationIngestionResult(null, Deduplicated: true);
        }

        var dedupKey = NormalizeOptional(request.DeduplicationKey);
        if (dedupKey is not null)
        {
            var existsInLocal = _context.Notifications.Local.Any(n =>
                n.RecipientUserId == request.RecipientUserId &&
                n.DeduplicationKey == dedupKey);

            if (existsInLocal || await _context.Notifications.AnyAsync(
                    n => n.RecipientUserId == request.RecipientUserId && n.DeduplicationKey == dedupKey,
                    cancellationToken))
            {
                return new NotificationIngestionResult(null, Deduplicated: true);
            }
        }

        var nowUtc = DateTime.UtcNow;
        var notification = new Notification
        {
            NotificationId = Guid.CreateVersion7(),
            RecipientUserId = request.RecipientUserId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            NotificationType = request.NotificationType.Trim(),
            SourceModule = string.IsNullOrWhiteSpace(request.SourceModule) ? "Notifications" : request.SourceModule.Trim(),
            SourceEventId = request.SourceEventId,
            DeduplicationKey = dedupKey,
            ResourceType = NormalizeOptional(request.ResourceType),
            ResourceId = request.ResourceId,
            ActionPath = NormalizeOptional(request.ActionPath),
            Priority = request.Priority,
            ExpiresAt = request.ExpiresAtUtc,
            IsRead = false,
            CreatedAt = nowUtc
        };

        _context.Notifications.Add(notification);

        Guid? deliveryId = null;
        if (request.EnqueueEmail)
        {
            var recipientEmail = NormalizeOptional(request.RecipientEmail);
            var recipientFullName = NormalizeOptional(request.RecipientFullName);

            if (recipientEmail is null || recipientFullName is null)
            {
                var userInfo = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.UserId == request.RecipientUserId && u.IsActive)
                    .Select(u => new { u.Email, u.FullName })
                    .FirstOrDefaultAsync(cancellationToken);

                recipientEmail ??= NormalizeOptional(userInfo?.Email);
                recipientFullName ??= NormalizeOptional(userInfo?.FullName);
            }

            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                var emailPayload = new NotificationEmailPayload(
                    Subject: string.IsNullOrWhiteSpace(request.EmailSubject)
                        ? $"[Huyết Mạch 175] {notification.Title}"
                        : request.EmailSubject.Trim(),
                    Title: notification.Title,
                    Content: notification.Content,
                    RecipientFullName: recipientFullName,
                    CategoryLabel: NormalizeOptional(request.EmailCategoryLabel),
                    Priority: request.Priority,
                    ActionPath: notification.ActionPath,
                    CtaLabel: NormalizeOptional(request.EmailCtaLabel),
                    Notice: NormalizeOptional(request.EmailNotice),
                    Metadata: request.EmailMetadata,
                    DigestSections: request.EmailDigestSections);

                var shouldSkip = _deliveryOptions.CurrentValue.ShouldSkipDestination(recipientEmail);
                var delivery = new NotificationDelivery
                {
                    DeliveryId = Guid.CreateVersion7(),
                    NotificationId = notification.NotificationId,
                    Channel = NotificationChannel.Email,
                    Destination = recipientEmail,
                    DeduplicationKey = dedupKey,
                    Status = shouldSkip ? NotificationDeliveryStatus.Skipped : NotificationDeliveryStatus.Pending,
                    AttemptCount = 0,
                    NextAttemptAt = nowUtc,
                    LastErrorCode = shouldSkip ? "SKIPPED_PLACEHOLDER_DOMAIN" : null,
                    LastErrorMessage = shouldSkip ? "Bỏ qua gửi SMTP cho tên miền nội bộ giả lập." : null,
                    PayloadJson = JsonSerializer.Serialize(emailPayload, JsonOptions),
                    CreatedAt = nowUtc
                };

                _context.NotificationDeliveries.Add(delivery);
                deliveryId = delivery.DeliveryId;
            }
        }

        return new NotificationIngestionResult(notification.NotificationId, Deduplicated: false, deliveryId);
    }

    public async Task<NotificationIngestionResult> IngestAsync(
        NotificationIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var staged = await StageAsync(request, cancellationToken);
        if (staged.Deduplicated)
        {
            return staged;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return staged;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogInformation(
                "Bỏ qua thông báo trùng lặp (Unique Violation) cho UserId={UserId}, Key={DeduplicationKey}.",
                request.RecipientUserId,
                request.DeduplicationKey);

            DetachStagedEntities(staged.NotificationId, staged.DeliveryId);
            return new NotificationIngestionResult(null, Deduplicated: true);
        }
    }

    public async Task<IReadOnlyList<NotificationIngestionResult>> StageForGroupAsync(
        NotificationGroupIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var recipients = await ResolveGroupRecipientsAsync(request, cancellationToken);
        if (recipients.Count == 0)
        {
            return [];
        }

        var results = new List<NotificationIngestionResult>(recipients.Count);
        var keyPrefix = NormalizeOptional(request.DeduplicationKeyPrefix);

        foreach (var recipient in recipients)
        {
            var singleRequest = new NotificationIngestionRequest(
                RecipientUserId: recipient.UserId,
                Title: request.Title,
                Content: request.Content,
                NotificationType: request.NotificationType,
                SourceModule: request.SourceModule,
                SourceEventId: request.SourceEventId,
                DeduplicationKey: keyPrefix is null ? null : $"{keyPrefix}:{recipient.UserId}",
                ResourceType: request.ResourceType,
                ResourceId: request.ResourceId,
                ActionPath: request.ActionPath,
                Priority: request.Priority,
                ExpiresAtUtc: request.ExpiresAtUtc,
                EnqueueEmail: request.EnqueueEmail,
                RecipientEmail: recipient.Email,
                EmailSubject: request.EmailSubject,
                RecipientFullName: recipient.FullName,
                EmailCategoryLabel: request.EmailCategoryLabel,
                EmailCtaLabel: request.EmailCtaLabel,
                EmailNotice: request.EmailNotice,
                EmailMetadata: request.EmailMetadata);

            results.Add(await StageAsync(singleRequest, cancellationToken));
        }

        return results;
    }

    public async Task<IReadOnlyList<NotificationIngestionResult>> IngestForGroupAsync(
        NotificationGroupIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = await StageForGroupAsync(request, cancellationToken);
        if (results.Any(r => !r.Deduplicated))
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogInformation(
                    "Phát hiện trùng lặp khi lưu thông báo nhóm (Prefix={KeyPrefix}).",
                    request.DeduplicationKeyPrefix);

                foreach (var item in results)
                {
                    DetachStagedEntities(item.NotificationId, item.DeliveryId);
                }
            }
        }

        return results;
    }

    public async Task<Guid?> StageDonorEmailAsync(
        DonorEmailNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeOptional(request.RecipientEmail);
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var dedupKey = NormalizeOptional(request.DeduplicationKey);
        if (dedupKey is not null)
        {
            var existsInLocal = _context.NotificationDeliveries.Local.Any(d =>
                d.Channel == NotificationChannel.Email &&
                string.Equals(d.Destination, email, StringComparison.OrdinalIgnoreCase) &&
                d.DeduplicationKey == dedupKey);

            if (existsInLocal || await _context.NotificationDeliveries.AnyAsync(
                    d => d.Channel == NotificationChannel.Email &&
                         d.Destination == email &&
                         d.DeduplicationKey == dedupKey,
                    cancellationToken))
            {
                return null;
            }
        }

        var nowUtc = DateTime.UtcNow;
        var payload = new NotificationEmailPayload(
            Subject: request.Subject.Trim(),
            Title: request.Title.Trim(),
            Content: request.Content.Trim(),
            RecipientFullName: NormalizeOptional(request.RecipientFullName),
            CategoryLabel: NormalizeOptional(request.CategoryLabel),
            Priority: NotificationPriority.Normal,
            ActionPath: NormalizeOptional(request.ActionPath),
            CtaLabel: NormalizeOptional(request.CtaLabel),
            Notice: NormalizeOptional(request.Notice),
            Metadata: request.Metadata);

        var shouldSkip = _deliveryOptions.CurrentValue.ShouldSkipDestination(email);
        var delivery = new NotificationDelivery
        {
            DeliveryId = Guid.CreateVersion7(),
            NotificationId = null,
            DonorId = request.DonorId,
            Channel = NotificationChannel.Email,
            Destination = email,
            DeduplicationKey = dedupKey,
            Status = shouldSkip ? NotificationDeliveryStatus.Skipped : NotificationDeliveryStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = nowUtc,
            LastErrorCode = shouldSkip ? "SKIPPED_PLACEHOLDER_DOMAIN" : null,
            LastErrorMessage = shouldSkip ? "Bỏ qua gửi SMTP cho tên miền nội bộ giả lập." : null,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CreatedAt = nowUtc
        };

        _context.NotificationDeliveries.Add(delivery);
        return delivery.DeliveryId;
    }

    public async Task<Guid?> EnqueueDonorEmailAsync(
        DonorEmailNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var deliveryId = await StageDonorEmailAsync(request, cancellationToken);
        if (deliveryId is null)
        {
            return null;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return deliveryId;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogInformation(
                "Bỏ qua email Donor trùng lặp tới {Email} (Key={DeduplicationKey}).",
                request.RecipientEmail,
                request.DeduplicationKey);

            DetachStagedEntities(null, deliveryId);
            return null;
        }
    }

    private async Task<List<(Guid UserId, string FullName, string? Email)>> ResolveGroupRecipientsAsync(
        NotificationGroupIngestionRequest request,
        CancellationToken cancellationToken)
    {
        var userIds = request.UserIds?.Where(id => id != Guid.Empty).Distinct().ToHashSet() ?? [];
        var roleCodes = request.RoleCodes?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        var permissionCode = NormalizeOptional(request.PermissionCode);

        var hasPrincipalCriteria = userIds.Count > 0 || roleCodes.Length > 0 || permissionCode is not null;
        if (!hasPrincipalCriteria && request.DepartmentId is null)
        {
            return [];
        }

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        if (request.DepartmentId is Guid deptId && deptId != Guid.Empty)
        {
            query = query.Where(u => u.DepartmentId == deptId);
        }

        if (hasPrincipalCriteria)
        {
            query = query.Where(u =>
                (userIds.Count > 0 && userIds.Contains(u.UserId)) ||
                (roleCodes.Length > 0 && u.UserRoles.Any(ur =>
                    ur.Role != null &&
                    ur.Role.IsActive &&
                    roleCodes.Contains(ur.Role.RoleCode))) ||
                (permissionCode != null && u.UserRoles.Any(ur =>
                    ur.Role != null &&
                    ur.Role.IsActive &&
                    ur.Role.RolePermissions.Any(rp =>
                        rp.Permission != null &&
                        rp.Permission.PermissionCode == permissionCode))));
        }

        if (request.ExcludeUserId is Guid excludeId && excludeId != Guid.Empty)
        {
            query = query.Where(u => u.UserId != excludeId);
        }

        var rows = await query
            .Select(u => new { u.UserId, u.FullName, u.Email })
            .ToListAsync(cancellationToken);

        return rows
            .DistinctBy(u => u.UserId)
            .Select(u => (u.UserId, u.FullName, u.Email))
            .ToList();
    }

    private void DetachStagedEntities(Guid? notificationId, Guid? deliveryId)
    {
        if (deliveryId.HasValue)
        {
            var deliveryEntry = _context.ChangeTracker.Entries<NotificationDelivery>()
                .FirstOrDefault(e => e.Entity.DeliveryId == deliveryId.Value);
            if (deliveryEntry is not null)
            {
                deliveryEntry.State = EntityState.Detached;
            }
        }

        if (notificationId.HasValue)
        {
            var notifEntry = _context.ChangeTracker.Entries<Notification>()
                .FirstOrDefault(e => e.Entity.NotificationId == notificationId.Value);
            if (notifEntry is not null)
            {
                notifEntry.State = EntityState.Detached;
            }
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
