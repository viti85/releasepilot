using ReleasePilot.Domain.Abstractions;

namespace ReleasePilot.Application.Abstractions;

public interface IEventBus
{
    Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct);
}
