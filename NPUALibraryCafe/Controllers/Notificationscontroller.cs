using LibCafe.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NPUALibraryCafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationsController(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var notifications = await _notificationRepository.GetByUserIdAsync(userId);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

        return Ok(new
        {
            notifications = notifications.Select(n => new
            {
                id = n.Notificationid,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                isRead = n.Isread,
                relatedId = n.Relatedid,
                createdAt = n.Createdat
            }),
            unreadCount
        });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification == null || notification.Userid != userId) return NotFound();

        await _notificationRepository.MarkAsReadAsync(id);
        return Ok(new { message = "Marked as read" });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _notificationRepository.MarkAllAsReadAsync(userId);
        return Ok(new { message = "All notifications marked as read" });
    }
}