using System.Collections.Concurrent;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Thread-safe in-memory queue — swap for Azure Service Bus in production
/// </summary>
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly ConcurrentQueue<Notification> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public int Count => _queue.Count;

    public Task EnqueueAsync(Notification notification)
    {
        _queue.Enqueue(notification);
        _signal.Release();
        return Task.CompletedTask;
    }

    public async Task<Notification?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _queue.TryDequeue(out var notification);
        return notification;
    }
}

/// <summary>
/// Thread-safe in-memory store — swap for SQL Server / CosmosDB in production
/// </summary>
public class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _store = new();

    public Task SaveAsync(Notification notification)
    {
        _store[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Notification notification)
    {
        _store[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<Notification?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var notification);
        return Task.FromResult(notification);
    }

    public Task<IEnumerable<Notification>> GetAllAsync(NotificationStatus? status = null)
    {
        var result = _store.Values.AsEnumerable();
        if (status.HasValue)
            result = result.Where(n => n.Status == status.Value);
        return Task.FromResult(result.OrderByDescending(n => n.CreatedAt).AsEnumerable());
    }
}
