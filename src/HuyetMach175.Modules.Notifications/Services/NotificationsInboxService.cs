using HuyetMach175.Api.Data;
using HuyetMach175.Modules.Notifications.DTOs;
using HuyetMach175.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HuyetMach175.Modules.Notifications.Services;

public interface INotificationsInboxService
{
    Task<NotificationInboxPageDto> GetInboxAsync(
        Guid currentUserId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UnreadCountResponse> GetUnreadCountAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<MarkNotificationReadResponse> MarkAsReadAsync(
        Guid currentUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<MarkAllNotificationsReadResponse> MarkAllAsReadAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}

public sealed class NotificationsInboxService : INotificationsInboxService
{
    private readonly ApplicationDbContext _context;

    public NotificationsInboxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationInboxPageDto> GetInboxAsync(
        Guid currentUserId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == currentUserId);

        var unreadCount = await baseQuery
            .CountAsync(n => !n.IsRead, cancellationToken);

        var filteredQuery = unreadOnly
            ? baseQuery.Where(n => !n.IsRead)
            : baseQuery;

        var totalCount = unreadOnly
            ? unreadCount
            : await baseQuery.CountAsync(cancellationToken);

        var items = await filteredQuery
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationId)
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(n => new NotificationItemDto(
                n.NotificationId,
                n.Title,
                n.Content,
                n.NotificationType,
                n.SourceModule,
                n.ResourceType,
                n.ResourceId,
                n.ActionPath,
                n.Priority,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt,
                n.ExpiresAt))
            .ToListAsync(cancellationToken);

        return new NotificationInboxPageDto(
            Items: items,
            TotalCount: totalCount,
            UnreadCount: unreadCount,
            PageNumber: safePageNumber,
            PageSize: safePageSize);
    }

    public async Task<UnreadCountResponse> GetUnreadCountAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var count = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == currentUserId && !n.IsRead, cancellationToken);

        return new UnreadCountResponse(count);
    }

    public async Task<MarkNotificationReadResponse> MarkAsReadAsync(
        Guid currentUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(
                n => n.NotificationId == notificationId && n.RecipientUserId == currentUserId,
                cancellationToken);

        if (notification is null)
        {
            throw new HttpException(StatusCodes.Status404NotFound, "Không tìm thấy thông báo.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt ??= DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new MarkNotificationReadResponse(
            notification.NotificationId,
            notification.IsRead,
            notification.ReadAt);
    }

    public async Task<MarkAllNotificationsReadResponse> MarkAllAsReadAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var updatedCount = await _context.Notifications
            .Where(n => n.RecipientUserId == currentUserId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, nowUtc),
                cancellationToken);

        return new MarkAllNotificationsReadResponse(updatedCount);
    }
}
