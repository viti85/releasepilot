namespace ReleasePilot.Domain.Events;

public sealed record PromotionCompletedEvent(
    Guid PromotionId,
    Guid ActingUserId) : DomainEvent(PromotionId, ActingUserId);
