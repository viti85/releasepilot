using FluentAssertions;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Exceptions;
using ReleasePilot.Domain.ValueObjects;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;
using Environment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Tests.Domain.Aggregates;

public class EnvironmentPipelineTests
{
    private static readonly ApplicationId AppId = ApplicationId.New();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly string[] ApproverRoles = ["approver"];

    private static Promotion CreateCompleted(AppVersion version, Environment env, Promotion[]? prerequisites = null)
    {
        var p = Promotion.Request(AppId, version, env, UserId, [], prerequisites ?? []);
        p.Approve(UserId, ApproverRoles);
        p.StartDeployment(UserId);
        p.Complete(UserId);
        return p;
    }

    [Fact]
    public void Dev_CanBeRequested_WithNoPrerequisites()
    {
        var version = new AppVersion("1.0.0");

        var act = () => Promotion.Request(AppId, version, Environment.Dev, UserId, [], []);

        act.Should().NotThrow();
    }

    [Fact]
    public void Staging_Requires_CompletedDev_ForSameVersion()
    {
        var version = new AppVersion("1.0.0");
        var completedDev = CreateCompleted(version, Environment.Dev);

        var act = () => Promotion.Request(AppId, version, Environment.Staging, UserId, [], [completedDev]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Production_Requires_CompletedStaging_ForSameVersion()
    {
        var version = new AppVersion("1.0.0");
        var completedDev = CreateCompleted(version, Environment.Dev);
        var completedStaging = CreateCompleted(version, Environment.Staging, [completedDev]);

        var act = () => Promotion.Request(AppId, version, Environment.Production, UserId, [], [completedStaging]);

        act.Should().NotThrow();
    }

    [Fact]
    public void CompletedPromotionForDifferentVersion_DoesNotUnblockNextEnvironment()
    {
        var v1 = new AppVersion("1.0.0");
        var v2 = new AppVersion("2.0.0");
        var completedDevForV1 = CreateCompleted(v1, Environment.Dev);

        var act = () => Promotion.Request(AppId, v2, Environment.Staging, UserId, [], [completedDevForV1]);

        act.Should().ThrowExactly<EnvironmentSkippedException>()
           .Which.Target.Should().Be(Environment.Staging);
    }
}
