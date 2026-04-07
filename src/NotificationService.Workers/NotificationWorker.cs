using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;

namespace NotificationService.Workers;

/// <summary>
/// Background worker that continuously dequeues and processes notifications.
/// Implements retry logic (max 3 attempts) and dead-letter on final failure.
/// </summary>
public class NotificationWorker : BackgroundService
{
    private const int MaxRetries = 3;
    private readonly IMessageQueue _queue;
    private readonly INotificationRepository _repository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(
        IMessageQueue queue,
        INotificationRepository repository,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationWorker> logger)
    {
        _queue = queue;
        _repository = repository;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker started. Listening for messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var notification = await _queue.DequeueAsync(stoppingToken);
                if (notification == null) continue;

                await ProcessWithRetryAsync(notification, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("NotificationWorker shutting down.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in NotificationWorker");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessWithRetryAsync(Notification notification, CancellationToken ct)
    {
        notification.Status = NotificationStatus.Processing;
        await _repository.UpdateAsync(notification);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

                await dispatcher.DispatchAsync(notification);

                // Success
                notification.Status = NotificationStatus.Delivered;
                notification.ProcessedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(notification);

                _logger.LogInformation("Notification {Id} delivered on attempt {Attempt}", 
                    notification.Id, attempt);
                return;
            }
            catch (Exception ex)
            {
                notification.RetryCount = attempt;
                notification.ErrorMessage = ex.Message;

                _logger.LogWarning("Notification {Id} failed on attempt {Attempt}/{Max}: {Error}",
                    notification.Id, attempt, MaxRetries, ex.Message);

                if (attempt < MaxRetries)
                {
                    // Exponential backoff: 2s, 4s, 8s
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogInformation("Retrying {Id} in {Delay}s...", notification.Id, delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                }
                else
                {
                    // Dead letter after max retries
                    notification.Status = NotificationStatus.DeadLettered;
                    notification.ProcessedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(notification);

                    _logger.LogError("Notification {Id} dead-lettered after {Max} attempts", 
                        notification.Id, MaxRetries);
                }
            }
        }
    }
}
