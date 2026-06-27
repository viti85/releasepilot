namespace ReleasePilot.Domain.Events;

public sealed record DeploymentStartedEvent(
    Guid PromotionId,
    Guid ActingUserId,
    Environment TargetEnvironment) : DomainEvent(PromotionId, ActingUserId);
