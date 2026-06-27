using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Infrastructure.Persistence.Entities;

public class PromotionStateTransitionEntity
{
    public Guid Id { get; set; }
    public PromotionId PromotionId { get; set; } = default!;
    public PromotionStatus Status { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid UserId { get; set; }

    public PromotionEntity Promotion { get; set; } = default!;
}
