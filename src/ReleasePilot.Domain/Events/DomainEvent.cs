using ReleasePilot.Domain.Abstractions;

namespace ReleasePilot.Domain.Events;

public abstract record DomainEvent(Guid PromotionId, Guid ActingUserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
