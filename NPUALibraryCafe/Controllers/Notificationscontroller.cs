using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;
        public NotificationsController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        // GET /api/Notifications - Get all my notifications
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.Userid == userId)
                .OrderByDescending(n => n.Createdat)
                .Take(50)
                .Select(n => new
                {
                    id = n.Notificationid,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    isRead = n.Isread,
                    relatedId = n.Relatedid,
                    createdAt = n.Createdat
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.Userid == userId && !n.Isread);

            return Ok(new { notifications, unreadCount });
        }

        // PUT /api/Notifications/{id}/read - Mark as read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Notificationid == id && n.Userid == userId);

            if (notification == null) return NotFound();

            notification.Isread = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Marked as read" });
        }

        // PUT /api/Notifications/read-all - Mark all as read
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            var unread = await _context.Notifications
                .Where(n => n.Userid == userId && !n.Isread)
                .ToListAsync();

            foreach (var n in unread) n.Isread = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Marked {unread.Count} notifications as read" });
        }

        // Internal helper - Create notification (used by other controllers)
        public static async Task CreateNotification(
            LibraryCafeDbContext context,
            int userId,
            string title,
            string message,
            string type,
            int? relatedId = null)
        {
            var notification = new Notification
            {
                Userid = userId,
                Title = title,
                Message = message,
                Type = type,
                Relatedid = relatedId,
                Createdat = DateTime.Now
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }
    }
}