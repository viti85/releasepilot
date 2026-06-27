namespace ReleasePilot.Domain.Events;

public sealed record PromotionRolledBackEvent(
    Guid PromotionId,
    Guid ActingUserId,
    string Reason) : DomainEvent(PromotionId, ActingUserId);
