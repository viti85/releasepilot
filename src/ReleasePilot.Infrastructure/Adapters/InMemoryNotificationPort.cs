using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Infrastructure.Adapters;

public class InMemoryNotificationPort(ILogger<InMemoryNotificationPort> logger) : INotificationPort
{
    public ConcurrentBag<string> NotificationsSent { get; } = new();

    public Task NotifyTerminalStateAsync(PromotionId id, PromotionStatus status, CancellationToken ct)
    {
        var message = $"Promotion {id.Value} reached terminal status {status}";
        logger.LogInformation("PromotionId: {PromotionId}, Terminal Status: {Status}", id.Value, status);
        NotificationsSent.Add(message);
        return Task.CompletedTask;
    }
}
