using Microsoft.AspNetCore.Mvc;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IMessageQueue _queue;
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IMessageQueue queue,
        INotificationRepository repository,
        ILogger<NotificationsController> logger)
    {
        _queue = queue;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Send a notification (Email, SMS, or Push)
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] NotificationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Recipient = request.Recipient,
            Subject = request.Subject,
            Body = request.Body,
            Status = NotificationStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        await _repository.SaveAsync(notification);
        await _queue.EnqueueAsync(notification);

        _logger.LogInformation("Notification {Id} queued for {Recipient}", notification.Id, notification.Recipient);

        return Accepted(new { notification.Id, Status = "Queued", Message = "Notification queued for delivery" });
    }

    /// <summary>
    /// Get notification status by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        var notification = await _repository.GetByIdAsync(id);
        if (notification == null)
            return NotFound(new { Message = $"Notification {id} not found" });

        return Ok(new
        {
            notification.Id,
            notification.Type,
            notification.Recipient,
            notification.Status,
            notification.CreatedAt,
            notification.ProcessedAt,
            notification.RetryCount,
            notification.ErrorMessage
        });
    }

    /// <summary>
    /// Get all notifications with optional status filter
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] NotificationStatus? status = null)
    {
        var notifications = await _repository.GetAllAsync(status);
        return Ok(notifications.Select(n => new
        {
            n.Id,
            n.Type,
            n.Recipient,
            n.Status,
            n.CreatedAt,
            n.ProcessedAt,
            n.RetryCount
        }));
    }

    /// <summary>
    /// Get queue stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var all = await _repository.GetAllAsync();
        return Ok(new
        {
            Total = all.Count(),
            Queued = all.Count(n => n.Status == NotificationStatus.Queued),
            Processing = all.Count(n => n.Status == NotificationStatus.Processing),
            Delivered = all.Count(n => n.Status == NotificationStatus.Delivered),
            Failed = all.Count(n => n.Status == NotificationStatus.Failed),
            DeadLettered = all.Count(n => n.Status == NotificationStatus.DeadLettered)
        });
    }
}
