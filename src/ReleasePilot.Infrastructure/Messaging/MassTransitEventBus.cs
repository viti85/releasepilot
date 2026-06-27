using MassTransit;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Domain.Abstractions;

namespace ReleasePilot.Infrastructure.Messaging;

public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public async Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var @event in events)
            await publishEndpoint.Publish(@event, @event.GetType(), ct);
    }
}
