using HuyetMach175.Api.Data.Enums;
using HuyetMach175.SharedKernel.Notifications;

namespace HuyetMach175.Modules.Notifications.Email;

public sealed record NotificationEmailPayload(
    string Subject,
    string Title,
    string Content,
    string? RecipientFullName = null,
    string? CategoryLabel = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    string? ActionPath = null,
    string? CtaLabel = null,
    string? Notice = null,
    IReadOnlyList<NotificationEmailMetaItem>? Metadata = null,
    IReadOnlyList<NotificationEmailDigestSection>? DigestSections = null);
