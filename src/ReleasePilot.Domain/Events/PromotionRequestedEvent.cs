namespace ReleasePilot.Domain.Events;

public sealed record PromotionRequestedEvent(
    Guid PromotionId,
    Guid ActingUserId,
    Guid ApplicationId,
    string AppVersion,
    Environment TargetEnvironment) : DomainEvent(PromotionId, ActingUserId);
