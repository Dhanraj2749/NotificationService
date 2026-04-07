using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;
using NotificationService.Infrastructure.Handlers;
using NotificationService.Infrastructure.Messaging;
using Xunit;

namespace NotificationService.Tests;

public class InMemoryMessageQueueTests
{
    [Fact]
    public async Task Enqueue_ShouldIncreaseCount()
    {
        var queue = new InMemoryMessageQueue();
        var notification = CreateNotification(NotificationType.Email);

        await queue.EnqueueAsync(notification);

        queue.Count.Should().Be(1);
    }

    [Fact]
    public async Task Dequeue_ShouldReturnEnqueuedNotification()
    {
        var queue = new InMemoryMessageQueue();
        var notification = CreateNotification(NotificationType.SMS);
        await queue.EnqueueAsync(notification);

        var result = await queue.DequeueAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
        result.Type.Should().Be(NotificationType.SMS);
    }

    [Fact]
    public async Task Enqueue_MultipleItems_ShouldMaintainFifoOrder()
    {
        var queue = new InMemoryMessageQueue();
        var first = CreateNotification(NotificationType.Email);
        var second = CreateNotification(NotificationType.SMS);

        await queue.EnqueueAsync(first);
        await queue.EnqueueAsync(second);

        var r1 = await queue.DequeueAsync(CancellationToken.None);
        var r2 = await queue.DequeueAsync(CancellationToken.None);

        r1!.Id.Should().Be(first.Id);
        r2!.Id.Should().Be(second.Id);
    }
}

public class NotificationDispatcherTests
{
    [Fact]
    public async Task Dispatch_ShouldRouteToCorrectHandler()
    {
        var emailHandler = new Mock<INotificationHandler>();
        emailHandler.Setup(h => h.Type).Returns(NotificationType.Email);
        emailHandler.Setup(h => h.HandleAsync(It.IsAny<Notification>())).ReturnsAsync(true);

        var smsHandler = new Mock<INotificationHandler>();
        smsHandler.Setup(h => h.Type).Returns(NotificationType.SMS);

        var logger = new Mock<ILogger<NotificationDispatcher>>();
        var dispatcher = new NotificationDispatcher(
            new[] { emailHandler.Object, smsHandler.Object }, logger.Object);

        var notification = CreateNotification(NotificationType.Email);
        await dispatcher.DispatchAsync(notification);

        emailHandler.Verify(h => h.HandleAsync(notification), Times.Once);
        smsHandler.Verify(h => h.HandleAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_UnknownType_ShouldThrow()
    {
        var logger = new Mock<ILogger<NotificationDispatcher>>();
        var dispatcher = new NotificationDispatcher(Enumerable.Empty<INotificationHandler>(), logger.Object);

        var notification = CreateNotification(NotificationType.Push);
        var act = async () => await dispatcher.DispatchAsync(notification);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task Save_ThenGetById_ShouldReturnSameNotification()
    {
        var repo = new InMemoryNotificationRepository();
        var notification = CreateNotification(NotificationType.Email);

        await repo.SaveAsync(notification);
        var result = await repo.GetByIdAsync(notification.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task Update_ShouldModifyExistingNotification()
    {
        var repo = new InMemoryNotificationRepository();
        var notification = CreateNotification(NotificationType.Email);
        await repo.SaveAsync(notification);

        notification.Status = NotificationStatus.Delivered;
        await repo.UpdateAsync(notification);

        var result = await repo.GetByIdAsync(notification.Id);
        result!.Status.Should().Be(NotificationStatus.Delivered);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ShouldReturnFiltered()
    {
        var repo = new InMemoryNotificationRepository();
        var n1 = CreateNotification(NotificationType.Email);
        n1.Status = NotificationStatus.Delivered;
        var n2 = CreateNotification(NotificationType.SMS);
        n2.Status = NotificationStatus.Failed;

        await repo.SaveAsync(n1);
        await repo.SaveAsync(n2);

        var delivered = await repo.GetAllAsync(NotificationStatus.Delivered);
        delivered.Should().HaveCount(1);
        delivered.First().Id.Should().Be(n1.Id);
    }
}

// Helper
file static class TestHelpers
{
    public static Notification CreateNotification(NotificationType type) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Recipient = "test@example.com",
        Subject = "Test Subject",
        Body = "Test Body",
        Status = NotificationStatus.Queued,
        CreatedAt = DateTime.UtcNow
    };
}

// Make helper accessible in file
file static class Extensions
{
    public static Notification CreateNotification(NotificationType type)
        => TestHelpers.CreateNotification(type);
}
