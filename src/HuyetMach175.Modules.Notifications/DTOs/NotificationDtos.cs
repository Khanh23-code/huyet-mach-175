using HuyetMach175.Api.Data.Enums;

namespace HuyetMach175.Modules.Notifications.DTOs;

public sealed record NotificationItemDto(
    Guid Id,
    string Title,
    string Content,
    string NotificationType,
    string SourceModule,
    string? ResourceType,
    Guid? ResourceId,
    string? ActionPath,
    NotificationPriority Priority,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt,
    DateTime? ExpiresAt);

public sealed record NotificationInboxPageDto(
    IReadOnlyList<NotificationItemDto> Items,
    int TotalCount,
    int UnreadCount,
    int PageNumber,
    int PageSize);

public sealed record UnreadCountResponse(int Count);

public sealed record MarkNotificationReadResponse(
    Guid Id,
    bool IsRead,
    DateTime? ReadAt);

public sealed record MarkAllNotificationsReadResponse(int UpdatedCount);
