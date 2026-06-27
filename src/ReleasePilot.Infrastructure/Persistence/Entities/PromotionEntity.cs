namespace ReleasePilot.Infrastructure.Persistence.Entities;

public class PromotionEntity
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Version { get; set; } = default!;
    public int TargetEnvironment { get; set; }
    public int Status { get; set; }
    public Guid RequestedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<PromotionStateTransitionEntity> StateTransitions { get; set; } = [];
}
