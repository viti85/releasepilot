namespace ReleasePilot.Infrastructure.Persistence.Entities;

public class PromotionStateTransitionEntity
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public int Status { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid UserId { get; set; }

    public PromotionEntity Promotion { get; set; } = default!;
}
