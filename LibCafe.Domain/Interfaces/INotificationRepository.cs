using LibCafe.Domain.Entities;

namespace LibCafe.Domain.Interfaces;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<Notification?> GetByIdAsync(int id);
    Task CreateAsync(Notification notification);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
}