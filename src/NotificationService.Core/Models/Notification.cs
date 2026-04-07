using System.ComponentModel.DataAnnotations;

namespace NotificationService.Core.Models;

public class Notification
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NotificationRequest
{
    [Required]
    public NotificationType Type { get; set; }

    [Required]
    public string Recipient { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;
}

public enum NotificationType
{
    Email,
    SMS,
    Push
}

public enum NotificationStatus
{
    Queued,
    Processing,
    Delivered,
    Failed,
    DeadLettered
}
