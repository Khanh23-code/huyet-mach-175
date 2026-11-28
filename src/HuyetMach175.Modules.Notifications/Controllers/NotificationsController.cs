using HuyetMach175.Modules.Notifications.DTOs;
using HuyetMach175.Modules.Notifications.Services;
using HuyetMach175.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuyetMach175.Modules.Notifications.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Tags("Notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationsInboxService _inboxService;

    public NotificationsController(INotificationsInboxService inboxService)
    {
        _inboxService = inboxService;
    }

    [HttpGet]
    [EndpointSummary("Lấy danh sách thông báo hộp thư In-App của người dùng hiện tại")]
    [ProducesResponseType(typeof(NotificationInboxPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotificationsAsync(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetUserId();
        var result = await _inboxService.GetInboxAsync(
            currentUserId,
            unreadOnly,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("unread-count")]
    [EndpointSummary("Đếm số lượng thông báo chưa đọc của người dùng hiện tại")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetUserId();
        var result = await _inboxService.GetUnreadCountAsync(currentUserId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    [EndpointSummary("Đánh dấu một thông báo cụ thể là đã đọc")]
    [ProducesResponseType(typeof(MarkNotificationReadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsReadAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetUserId();
        var result = await _inboxService.MarkAsReadAsync(currentUserId, id, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("read-all")]
    [EndpointSummary("Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc")]
    [ProducesResponseType(typeof(MarkAllNotificationsReadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetUserId();
        var result = await _inboxService.MarkAllAsReadAsync(currentUserId, cancellationToken);
        return Ok(result);
    }
}
