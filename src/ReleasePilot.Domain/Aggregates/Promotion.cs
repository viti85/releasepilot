using ReleasePilot.Domain.Abstractions;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.Events;
using ReleasePilot.Domain.Exceptions;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Domain.Aggregates;

public sealed class Promotion : AggregateRoot
{
    private readonly List<PromotionStateTransition> _stateHistory = [];

    public PromotionId Id { get; private set; } = default!;
    public ApplicationId ApplicationId { get; private set; } = default!;
    public AppVersion Version { get; private set; } = default!;
    public Environment TargetEnvironment { get; private set; }
    public PromotionStatus Status { get; private set; }
    public Guid RequestedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<PromotionStateTransition> StateHistory => _stateHistory.AsReadOnly();

    private Promotion() { }

    public static Promotion Request(
        ApplicationId applicationId,
        AppVersion version,
        Environment target,
        Guid requestedByUserId,
        IReadOnlyList<Promotion> activePromotions,
        IReadOnlyList<Promotion> completedPromotionsForApp)
    {
        if (target == Environment.Staging &&
            !completedPromotionsForApp.Any(p =>
                p.TargetEnvironment == Environment.Dev &&
                p.Status == PromotionStatus.Completed &&
                p.Version == version))
        {
            throw new EnvironmentSkippedException(target, Environment.Dev);
        }

        if (target == Environment.Production &&
            !completedPromotionsForApp.Any(p =>
                p.TargetEnvironment == Environment.Staging &&
                p.Status == PromotionStatus.Completed &&
                p.Version == version))
        {
            throw new EnvironmentSkippedException(target, Environment.Staging);
        }

        if (activePromotions.Any(p =>
                p.ApplicationId == applicationId &&
                p.TargetEnvironment == target))
        {
            throw new ConcurrentPromotionException(applicationId, target);
        }

        var id = PromotionId.New();
        var now = DateTime.UtcNow;

        var promotion = new Promotion
        {
            Id = id,
            ApplicationId = applicationId,
            Version = version,
            TargetEnvironment = target,
            Status = PromotionStatus.Pending,
            RequestedBy = requestedByUserId,
            RequestedAt = now
        };

        promotion._stateHistory.Add(new PromotionStateTransition(PromotionStatus.Pending, now, requestedByUserId));
        promotion.Raise(new PromotionRequestedEvent(id.Value, requestedByUserId, applicationId.Value, version.Value, target));

        return promotion;
    }

    public void Approve(Guid approverId)
    {
        GuardNotTerminal();

        if (Status != PromotionStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a promotion in '{Status}' status.");
                
        ApprovedBy = approverId;
        Transition(PromotionStatus.Approved, approverId);
        Raise(new PromotionApprovedEvent(Id.Value, approverId));
    }

    public void StartDeployment(Guid userId)
    {
        GuardNotTerminal();

        if (Status != PromotionStatus.Approved)
            throw new InvalidOperationException($"Cannot start deployment for a promotion in '{Status}' status.");

        Transition(PromotionStatus.InProgress, userId);
        Raise(new DeploymentStartedEvent(Id.Value, userId, TargetEnvironment));
    }

    public void Complete(Guid userId)
    {
        GuardNotTerminal();

        if (Status != PromotionStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete a promotion in '{Status}' status.");

        CompletedAt = DateTime.UtcNow;
        Transition(PromotionStatus.Completed, userId);
        Raise(new PromotionCompletedEvent(Id.Value, userId));
    }

    public void Rollback(Guid userId, string reason)
    {
        GuardNotTerminal();

        if (Status != PromotionStatus.InProgress)
            throw new InvalidOperationException($"Cannot rollback a promotion in '{Status}' status.");

        CompletedAt = DateTime.UtcNow;
        Transition(PromotionStatus.RolledBack, userId);
        Raise(new PromotionRolledBackEvent(Id.Value, userId, reason));
    }

    public void Cancel(Guid userId)
    {
        GuardNotTerminal();

        if (Status is not (PromotionStatus.Pending or PromotionStatus.Approved))
            throw new InvalidOperationException($"Cannot cancel a promotion in '{Status}' status.");

        Transition(PromotionStatus.Cancelled, userId);
        Raise(new PromotionCancelledEvent(Id.Value, userId, null));
    }

    private void GuardNotTerminal()
    {
        if (Status is PromotionStatus.Completed or PromotionStatus.RolledBack or PromotionStatus.Cancelled)
            throw new ImmutablePromotionException(Id, Status);
    }

    public static Promotion Reconstitute(
        PromotionId id,
        ApplicationId applicationId,
        AppVersion version,
        Environment targetEnvironment,
        PromotionStatus status,
        Guid requestedBy,
        Guid? approvedBy,
        DateTime requestedAt,
        DateTime? completedAt,
        IEnumerable<PromotionStateTransition> stateHistory)
    {
        var promotion = new Promotion
        {
            Id = id,
            ApplicationId = applicationId,
            Version = version,
            TargetEnvironment = targetEnvironment,
            Status = status,
            RequestedBy = requestedBy,
            ApprovedBy = approvedBy,
            RequestedAt = requestedAt,
            CompletedAt = completedAt,
        };
        promotion._stateHistory.AddRange(stateHistory);
        return promotion;
    }

    private void Transition(PromotionStatus newStatus, Guid userId)
    {
        Status = newStatus;
        _stateHistory.Add(new PromotionStateTransition(newStatus, DateTime.UtcNow, userId));
    }
}
