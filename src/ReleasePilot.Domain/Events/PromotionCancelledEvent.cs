namespace ReleasePilot.Domain.Events;

public sealed record PromotionCancelledEvent(
    Guid PromotionId,
    Guid ActingUserId,
    string? Reason) : DomainEvent(PromotionId, ActingUserId);
