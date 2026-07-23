using FamilyHub.Api.DTOs.Notifications;
using FamilyHub.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await _notifications.GetForUserAsync(CurrentUserId));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount() =>
        Ok(new UnreadCountResponse(await _notifications.GetUnreadCountAsync(CurrentUserId)));

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        var result = await _notifications.MarkAsReadAsync(CurrentUserId, notificationId);
        return result.Succeeded ? NoContent() : Error(result);
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notifications.MarkAllAsReadAsync(CurrentUserId);
        return NoContent();
    }

    [HttpDelete("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllRead()
    {
        await _notifications.DeleteAllReadAsync(CurrentUserId);
        return NoContent();
    }

    [HttpDelete("{notificationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid notificationId)
    {
        var result = await _notifications.DeleteAsync(CurrentUserId, notificationId);
        return result.Succeeded ? NoContent() : Error(result);
    }
}
