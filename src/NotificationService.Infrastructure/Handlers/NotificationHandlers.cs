using Microsoft.Extensions.Logging;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;

namespace NotificationService.Infrastructure.Handlers;

/// <summary>
/// Email channel handler — replace SendAsync body with real SMTP/SendGrid in production
/// </summary>
public class EmailNotificationHandler : INotificationHandler
{
    private readonly ILogger<EmailNotificationHandler> _logger;
    public NotificationType Type => NotificationType.Email;

    public EmailNotificationHandler(ILogger<EmailNotificationHandler> logger)
        => _logger = logger;

    public async Task<bool> HandleAsync(Notification notification)
    {
        await Task.Delay(100); // Simulate network call
        _logger.LogInformation("[EMAIL] Sent to {Recipient} | Subject: {Subject}", 
            notification.Recipient, notification.Subject);
        return true;
    }
}

/// <summary>
/// SMS channel handler — replace SendAsync body with real Twilio/Azure SMS in production
/// </summary>
public class SmsNotificationHandler : INotificationHandler
{
    private readonly ILogger<SmsNotificationHandler> _logger;
    public NotificationType Type => NotificationType.SMS;

    public SmsNotificationHandler(ILogger<SmsNotificationHandler> logger)
        => _logger = logger;

    public async Task<bool> HandleAsync(Notification notification)
    {
        await Task.Delay(80);
        _logger.LogInformation("[SMS] Sent to {Recipient} | Body: {Body}", 
            notification.Recipient, notification.Body);
        return true;
    }
}

/// <summary>
/// Push notification handler — replace with Azure Notification Hubs in production
/// </summary>
public class PushNotificationHandler : INotificationHandler
{
    private readonly ILogger<PushNotificationHandler> _logger;
    public NotificationType Type => NotificationType.Push;

    public PushNotificationHandler(ILogger<PushNotificationHandler> logger)
        => _logger = logger;

    public async Task<bool> HandleAsync(Notification notification)
    {
        await Task.Delay(60);
        _logger.LogInformation("[PUSH] Sent to {Recipient} | Subject: {Subject}", 
            notification.Recipient, notification.Subject);
        return true;
    }
}

/// <summary>
/// Routes notifications to the correct handler by type
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationHandler> _handlers;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationHandler> handlers,
        ILogger<NotificationDispatcher> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task DispatchAsync(Notification notification)
    {
        var handler = _handlers.FirstOrDefault(h => h.Type == notification.Type);
        if (handler == null)
        {
            _logger.LogWarning("No handler found for type {Type}", notification.Type);
            throw new InvalidOperationException($"No handler for {notification.Type}");
        }

        await handler.HandleAsync(notification);
    }
}
