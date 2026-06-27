namespace ReleasePilot.Domain.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    Guid PromotionId { get; }
    Guid ActingUserId { get; }
}
