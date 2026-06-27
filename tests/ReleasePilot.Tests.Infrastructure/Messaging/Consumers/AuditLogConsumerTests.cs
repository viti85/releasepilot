using System.Text.Json;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ReleasePilot.Domain.Abstractions;
using ReleasePilot.Domain.Events;
using ReleasePilot.Infrastructure.Messaging.Consumers;
using ReleasePilot.Infrastructure.Persistence;

namespace ReleasePilot.Tests.Infrastructure.Messaging.Consumers;

public class AuditLogConsumerTests
{
    [Fact]
    public async Task Consume_ValidDomainEvent_PersistsToAuditLogTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReleasePilotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new ReleasePilotDbContext(options);
        var consumer = new AuditLogConsumer(dbContext);

        var promotionId = Guid.NewGuid();
        var actingUserId = Guid.NewGuid();
        var domainEvent = new PromotionCompletedEvent(promotionId, actingUserId);

        var context = Substitute.For<ConsumeContext<IDomainEvent>>();
        context.Message.Returns(domainEvent);
        context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await consumer.Consume(context);

        // Assert
        var logs = await dbContext.AuditLog.ToListAsync();
        logs.Should().ContainSingle();

        var log = logs.First();
        log.Id.Should().NotBeEmpty();
        log.EventType.Should().Be(nameof(PromotionCompletedEvent));
        log.PromotionId.Value.Should().Be(promotionId);
        log.ActingUserId.Should().Be(actingUserId);
        log.OccurredAt.Should().BeCloseTo(domainEvent.OccurredAt, TimeSpan.FromMilliseconds(100));

        // Check JSON payload
        var deserializedPayload = JsonSerializer.Deserialize<PromotionCompletedEvent>(log.Payload);
        deserializedPayload.Should().NotBeNull();
        deserializedPayload!.PromotionId.Should().Be(promotionId);
        deserializedPayload.ActingUserId.Should().Be(actingUserId);
        deserializedPayload.EventId.Should().Be(domainEvent.EventId);
    }
}
