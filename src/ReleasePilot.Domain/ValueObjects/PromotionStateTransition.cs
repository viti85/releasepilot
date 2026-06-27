using ReleasePilot.Domain.Enums;

namespace ReleasePilot.Domain.ValueObjects;

public sealed record PromotionStateTransition(
    PromotionStatus Status,
    DateTime Timestamp,
    Guid UserId);
