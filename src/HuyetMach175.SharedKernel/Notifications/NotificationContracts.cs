using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.SharedKernel.Notifications;

public sealed record NotificationEmailMetaItem(
    string Label,
    string Value);

public sealed record NotificationEmailDigestSection(
    string Title,
    int Count,
    IReadOnlyList<string> Items,
    string? ActionPath = null,
    string? CtaLabel = null);

public sealed record NotificationIngestionRequest(
    Guid RecipientUserId,
    string Title,
    string Content,
    string NotificationType,
    string SourceModule = "Notifications",
    Guid? SourceEventId = null,
    string? DeduplicationKey = null,
    string? ResourceType = null,
    Guid? ResourceId = null,
    string? ActionPath = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    DateTime? ExpiresAtUtc = null,
    bool EnqueueEmail = false,
    string? RecipientEmail = null,
    string? EmailSubject = null,
    string? RecipientFullName = null,
    string? EmailCategoryLabel = null,
    string? EmailCtaLabel = null,
    string? EmailNotice = null,
    IReadOnlyList<NotificationEmailMetaItem>? EmailMetadata = null,
    IReadOnlyList<NotificationEmailDigestSection>? EmailDigestSections = null);

public sealed record NotificationGroupIngestionRequest(
    string Title,
    string Content,
    string NotificationType,
    string SourceModule = "Notifications",
    IReadOnlyCollection<Guid>? UserIds = null,
    IReadOnlyCollection<string>? RoleCodes = null,
    string? PermissionCode = null,
    Guid? DepartmentId = null,
    Guid? ExcludeUserId = null,
    Guid? SourceEventId = null,
    string? DeduplicationKeyPrefix = null,
    string? ResourceType = null,
    Guid? ResourceId = null,
    string? ActionPath = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    DateTime? ExpiresAtUtc = null,
    bool EnqueueEmail = false,
    string? EmailSubject = null,
    string? EmailCategoryLabel = null,
    string? EmailCtaLabel = null,
    string? EmailNotice = null,
    IReadOnlyList<NotificationEmailMetaItem>? EmailMetadata = null);

public sealed record DonorEmailNotificationRequest(
    Guid? DonorId,
    string RecipientEmail,
    string RecipientFullName,
    string Subject,
    string Title,
    string Content,
    string? DeduplicationKey = null,
    string? CategoryLabel = "Hiến máu tình nguyện",
    string? ActionPath = null,
    string? CtaLabel = null,
    string? Notice = null,
    IReadOnlyList<NotificationEmailMetaItem>? Metadata = null);

public sealed record NotificationIngestionResult(
    Guid? NotificationId,
    bool Deduplicated,
    Guid? DeliveryId = null);

public interface INotificationIngestionService
{
    Task<NotificationIngestionResult> StageAsync(
        NotificationIngestionRequest request,
        CancellationToken cancellationToken = default);

    Task<NotificationIngestionResult> IngestAsync(
        NotificationIngestionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationIngestionResult>> StageForGroupAsync(
        NotificationGroupIngestionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationIngestionResult>> IngestForGroupAsync(
        NotificationGroupIngestionRequest request,
        CancellationToken cancellationToken = default);

    Task<Guid?> StageDonorEmailAsync(
        DonorEmailNotificationRequest request,
        CancellationToken cancellationToken = default);

    Task<Guid?> EnqueueDonorEmailAsync(
        DonorEmailNotificationRequest request,
        CancellationToken cancellationToken = default);
}
