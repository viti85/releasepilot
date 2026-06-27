namespace ReleasePilot.Domain.Events;

public sealed record PromotionApprovedEvent(
    Guid PromotionId,
    Guid ActingUserId) : DomainEvent(PromotionId, ActingUserId);
