using NotificationService.Core.Models;

namespace NotificationService.Core.Interfaces;

public interface IMessageQueue
{
    Task EnqueueAsync(Notification notification);
    Task<Notification?> DequeueAsync(CancellationToken cancellationToken);
    int Count { get; }
}

public interface INotificationHandler
{
    NotificationType Type { get; }
    Task<bool> HandleAsync(Notification notification);
}

public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification);
}

public interface INotificationRepository
{
    Task SaveAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Notification>> GetAllAsync(NotificationStatus? status = null);
}
