using FluentAssertions;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.Exceptions;
using ReleasePilot.Domain.ValueObjects;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;
using Environment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Tests.Domain.Aggregates;

public class PromotionInvariantTests
{
    private static readonly ApplicationId AppId = ApplicationId.New();
    private static readonly AppVersion V1 = new("1.0.0");
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly string[] ApproverRoles = ["approver"];

    private static Promotion RequestDev() =>
        Promotion.Request(AppId, V1, Environment.Dev, UserId, [], []);

    private static Promotion CreateCompleted(Environment env, Promotion[]? prerequisites = null)
    {
        var p = Promotion.Request(AppId, V1, env, UserId, [], prerequisites ?? []);
        p.Approve(UserId);
        p.StartDeployment(UserId);
        p.Complete(UserId);
        return p;
    }

    [Fact]
    public void Request_ToStaging_WithoutCompletedDev_ThrowsEnvironmentSkippedException()
    {
        var act = () => Promotion.Request(AppId, V1, Environment.Staging, UserId, [], []);

        act.Should().ThrowExactly<EnvironmentSkippedException>()
           .Which.Target.Should().Be(Environment.Staging);
    }

    [Fact]
    public void Request_ToProduction_WithoutCompletedStaging_ThrowsEnvironmentSkippedException()
    {
        var completedDev = CreateCompleted(Environment.Dev);

        var act = () => Promotion.Request(AppId, V1, Environment.Production, UserId, [], [completedDev]);

        act.Should().ThrowExactly<EnvironmentSkippedException>()
           .Which.Target.Should().Be(Environment.Production);
    }

    [Fact]
    public void Request_WhenActivePromotionExistsForSameAppAndEnv_ThrowsConcurrentPromotionException()
    {
        var existing = RequestDev();

        var act = () => Promotion.Request(AppId, V1, Environment.Dev, UserId, [existing], []);

        act.Should().ThrowExactly<ConcurrentPromotionException>()
           .Which.ApplicationId.Should().Be(AppId);
    }

    [Fact]
    public void Mutate_CompletedPromotion_ThrowsImmutablePromotionException()
    {
        var promotion = CreateCompleted(Environment.Dev);

        var act = () => promotion.Cancel(UserId);

        act.Should().ThrowExactly<ImmutablePromotionException>()
           .Which.Status.Should().Be(PromotionStatus.Completed);
    }

    [Fact]
    public void Mutate_CancelledPromotion_ThrowsImmutablePromotionException()
    {
        var promotion = RequestDev();
        promotion.Cancel(UserId);

        var act = () => promotion.Cancel(UserId);

        act.Should().ThrowExactly<ImmutablePromotionException>()
           .Which.Status.Should().Be(PromotionStatus.Cancelled);
    }

    [Fact]
    public void Mutate_RolledBackPromotion_ThrowsImmutablePromotionException()
    {
        var promotion = RequestDev();
        promotion.Approve(UserId);
        promotion.StartDeployment(UserId);
        promotion.Rollback(UserId, "critical bug found");

        var act = () => promotion.Cancel(UserId);

        act.Should().ThrowExactly<ImmutablePromotionException>()
           .Which.Status.Should().Be(PromotionStatus.RolledBack);
    }
}
