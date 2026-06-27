using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;
using ReleasePilot.Infrastructure.Adapters;

namespace ReleasePilot.Tests.Infrastructure.Adapters;

public class InMemoryAdapterTests
{
    [Fact]
    public async Task InMemoryDeploymentPort_TriggerAsync_LogsAndFinishes()
    {
        // Arrange
        var logger = Substitute.For<ILogger<InMemoryDeploymentPort>>();
        var port = new InMemoryDeploymentPort(logger);
        var promotionId = PromotionId.New();

        // Act
        var act = () => port.TriggerAsync(promotionId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InMemoryNotificationPort_NotifyTerminalStateAsync_LogsAndStoresNotification()
    {
        // Arrange
        var logger = Substitute.For<ILogger<InMemoryNotificationPort>>();
        var port = new InMemoryNotificationPort(logger);
        var promotionId = PromotionId.New();
        var status = PromotionStatus.Completed;

        // Act
        await port.NotifyTerminalStateAsync(promotionId, status, CancellationToken.None);

        // Assert
        port.NotificationsSent.Should().ContainSingle(m => m.Contains(promotionId.Value.ToString()) && m.Contains(status.ToString()));
    }

    [Fact]
    public async Task InMemoryIssueTrackerPort_GetLinkedItemsAsync_ReturnsStubs()
    {
        // Arrange
        var port = new InMemoryIssueTrackerPort();
        var promotionId = PromotionId.New();

        // Act
        var result = await port.GetLinkedItemsAsync(promotionId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(x => x.Id == "ISSUE-1" && x.Title == "Add login feature");
        result.Should().ContainSingle(x => x.Id == "ISSUE-2" && x.Title == "Fix signup bug");
    }
}
