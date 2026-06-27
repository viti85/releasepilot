using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;
using DomainEnvironment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Infrastructure.Persistence.Entities;

public class PromotionEntity
{
    public PromotionId Id { get; set; } = default!;
    public ApplicationId ApplicationId { get; set; } = default!;
    public AppVersion Version { get; set; } = default!;
    public DomainEnvironment TargetEnvironment { get; set; }
    public PromotionStatus Status { get; set; }
    public Guid RequestedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<PromotionStateTransitionEntity> StateTransitions { get; set; } = [];
}
