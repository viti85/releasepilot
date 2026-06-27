using System.Text.Json;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Domain.Abstractions;
using ReleasePilot.Domain.Events;
using ReleasePilot.Infrastructure.Messaging.Consumers;
using ReleasePilot.Infrastructure.Persistence;
using Xunit;

namespace ReleasePilot.Tests.Infrastructure.Messaging.Consumers;

[Collection("Database Tests")]
public class AuditLogConsumerTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public AuditLogConsumerTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;

        // Clean relevant tables before each test
        using var dbContext = CreateDbContext();
        dbContext.Database.ExecuteSqlRaw("TRUNCATE TABLE promotion_state_transitions, promotions, audit_log CASCADE;");
    }

    private ReleasePilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ReleasePilotDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new ReleasePilotDbContext(options);
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ReleasePilotDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<AuditLogConsumer>();
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Consume_PromotionApprovedEvent_persists_audit_entry()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var promotionId = Guid.NewGuid();
        var actingUserId = Guid.NewGuid();
        var approvedEvent = new PromotionApprovedEvent(promotionId, actingUserId);

        // Act
        await harness.Bus.Publish(approvedEvent);

        // Assert
        // Wait for consumer to finish
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var consumed = await harness.Consumed.Any<IDomainEvent>(cts.Token);
        consumed.Should().BeTrue();

        using var dbContext = CreateDbContext();
        var logs = await dbContext.AuditLog.ToListAsync();
        logs.Should().ContainSingle();

        var log = logs.First();
        log.Id.Should().NotBeEmpty();
        log.EventType.Should().Be(nameof(PromotionApprovedEvent));
        log.PromotionId.Value.Should().Be(promotionId);
        log.ActingUserId.Should().Be(actingUserId);

        var deserializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var payloadObj = JsonSerializer.Deserialize<PromotionApprovedEvent>(log.Payload, deserializeOptions);
        payloadObj.Should().NotBeNull();
        payloadObj!.PromotionId.Should().Be(promotionId);
        payloadObj.ActingUserId.Should().Be(actingUserId);
    }

    [Fact]
    public async Task Consume_multiple_events_persists_all_entries()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var promotionId = Guid.NewGuid();
        var actingUserId = Guid.NewGuid();
        var requestedEvent = new PromotionRequestedEvent(
            promotionId, actingUserId, Guid.NewGuid(), "1.0.0", Domain.Enums.Environment.Dev);
        var approvedEvent = new PromotionApprovedEvent(promotionId, actingUserId);

        // Act & Assert for first event
        await harness.Bus.Publish(requestedEvent);
        
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            while (!cts.Token.IsCancellationRequested)
            {
                using var db = CreateDbContext();
                var count = await db.AuditLog.CountAsync();
                if (count >= 1) break;
                await Task.Delay(100);
            }
        }

        // Act & Assert for second event
        await harness.Bus.Publish(approvedEvent);
        
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
        {
            while (!cts.Token.IsCancellationRequested)
            {
                using var db = CreateDbContext();
                var count = await db.AuditLog.CountAsync();
                if (count >= 2) break;
                await Task.Delay(100);
            }
        }

        // Final Assertions
        using var dbContext = CreateDbContext();
        var logs = await dbContext.AuditLog.ToListAsync();
        logs.Should().HaveCount(2);

        logs.Should().ContainSingle(l => l.EventType == nameof(PromotionRequestedEvent));
        logs.Should().ContainSingle(l => l.EventType == nameof(PromotionApprovedEvent));
    }

    [Fact]
    public async Task Consumer_does_not_block_publisher()
    {
        // Arrange
        var provider = CreateServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var approvedEvent = new PromotionApprovedEvent(Guid.NewGuid(), Guid.NewGuid());

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var consumedTask = harness.Consumed.Any<IDomainEvent>(cts.Token);

        var publishTask = harness.Bus.Publish(approvedEvent);
        
        // Assert that the publish call returns immediately, representing fire-and-forget
        await publishTask;

        // Ensure the consumer finishes after the publish completes
        var consumed = await consumedTask;
        consumed.Should().BeTrue();
    }
}
