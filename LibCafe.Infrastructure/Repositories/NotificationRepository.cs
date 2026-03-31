using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibCafe.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly LibraryCafeDbContext _context;

    public NotificationRepository(LibraryCafeDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId) =>
        await _context.Notifications
            .Where(n => n.Userid == userId)
            .OrderByDescending(n => n.Createdat)
            .Take(50)
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(int userId) =>
        await _context.Notifications
            .CountAsync(n => n.Userid == userId && !n.Isread);

    public async Task<Notification?> GetByIdAsync(int id) =>
        await _context.Notifications.FindAsync(id);

    public async Task CreateAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await GetByIdAsync(notificationId);
        if (notification == null) return;
        notification.Isread = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.Userid == userId && !n.Isread)
            .ToListAsync();
        foreach (var n in unread) n.Isread = true;
        await _context.SaveChangesAsync();
    }
}