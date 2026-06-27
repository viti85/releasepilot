using FluentAssertions;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.Events;
using ReleasePilot.Domain.ValueObjects;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;
using Environment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Tests.Domain.Aggregates;

public class PromotionStateMachineTests
{
    private static readonly ApplicationId AppId = ApplicationId.New();
    private static readonly AppVersion V1 = new("1.0.0");
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly string[] ApproverRoles = ["approver"];

    private static Promotion RequestDev() =>
        Promotion.Request(AppId, V1, Environment.Dev, UserId, [], []);

    [Fact]
    public void HappyPath_RequestApproveThenStartDeploymentThenComplete_StatusIsCompleted()
    {
        var promotion = RequestDev();

        promotion.Approve(UserId, ApproverRoles);
        promotion.StartDeployment(UserId);
        promotion.Complete(UserId);

        promotion.Status.Should().Be(PromotionStatus.Completed);
        promotion.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void RollbackPath_RequestApproveThenStartDeploymentThenRollback_StatusIsRolledBack()
    {
        var promotion = RequestDev();
        promotion.Approve(UserId, ApproverRoles);
        promotion.StartDeployment(UserId);

        promotion.Rollback(UserId, "deployment failed");

        promotion.Status.Should().Be(PromotionStatus.RolledBack);
        promotion.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_FromPendingState_StatusIsCancelled()
    {
        var promotion = RequestDev();

        promotion.Cancel(UserId);

        promotion.Status.Should().Be(PromotionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromApprovedState_StatusIsCancelled()
    {
        var promotion = RequestDev();
        promotion.Approve(UserId, ApproverRoles);

        promotion.Cancel(UserId);

        promotion.Status.Should().Be(PromotionStatus.Cancelled);
    }

    [Fact]
    public void EachTransition_EmitsCorrectDomainEventType()
    {
        var promotion = RequestDev();
        promotion.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PromotionRequestedEvent>();

        promotion.Approve(UserId, ApproverRoles);
        promotion.DomainEvents.Should().HaveCount(2);
        promotion.DomainEvents.Last().Should().BeOfType<PromotionApprovedEvent>();

        promotion.StartDeployment(UserId);
        promotion.DomainEvents.Should().HaveCount(3);
        promotion.DomainEvents.Last().Should().BeOfType<DeploymentStartedEvent>();

        promotion.Complete(UserId);
        promotion.DomainEvents.Should().HaveCount(4);
        promotion.DomainEvents.Last().Should().BeOfType<PromotionCompletedEvent>();

        var rollbackPromotion = RequestDev();
        rollbackPromotion.Approve(UserId, ApproverRoles);
        rollbackPromotion.StartDeployment(UserId);
        rollbackPromotion.Rollback(UserId, "deployment failed");
        rollbackPromotion.DomainEvents.Last().Should().BeOfType<PromotionRolledBackEvent>();

        var cancelPromotion = RequestDev();
        cancelPromotion.Cancel(UserId);
        cancelPromotion.DomainEvents.Last().Should().BeOfType<PromotionCancelledEvent>();
    }

    [Fact]
    public void StateHistory_RecordsEachTransitionWithCorrectStatusAndTimestamp()
    {
        var before = DateTime.UtcNow;
        var promotion = RequestDev();
        promotion.Approve(UserId, ApproverRoles);
        promotion.StartDeployment(UserId);
        promotion.Complete(UserId);
        var after = DateTime.UtcNow;

        promotion.StateHistory.Should().HaveCount(4);
        promotion.StateHistory[0].Status.Should().Be(PromotionStatus.Pending);
        promotion.StateHistory[1].Status.Should().Be(PromotionStatus.Approved);
        promotion.StateHistory[2].Status.Should().Be(PromotionStatus.InProgress);
        promotion.StateHistory[3].Status.Should().Be(PromotionStatus.Completed);

        promotion.StateHistory.Should().AllSatisfy(t =>
            t.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after));
    }
}
