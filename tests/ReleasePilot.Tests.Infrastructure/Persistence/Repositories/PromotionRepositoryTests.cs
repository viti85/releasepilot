using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.ValueObjects;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Infrastructure.Persistence.Repositories;
using Xunit;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;

namespace ReleasePilot.Tests.Infrastructure.Persistence.Repositories;

[Collection("Database Tests")]
public class PromotionRepositoryTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public PromotionRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
        
        // Clean relevant tables before each test
        using var dbContext = CreateDbContext();
        dbContext.Database.ExecuteSqlRaw("TRUNCATE TABLE promotion_state_transitions, promotions, audit_log CASCADE;");
    }

    private ReleasePilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ReleasePilotDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new ReleasePilotDbContext(options);
    }

    [Fact]
    public async Task SaveAsync_and_GetById_roundtrip()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new PromotionRepository(dbContext);

        var appId = new ApplicationId(Guid.NewGuid());
        var version = new AppVersion("1.0.0");
        var requestedBy = Guid.NewGuid();
        var promotion = Promotion.Request(appId, version, Domain.Enums.Environment.Dev, requestedBy, [], []);

        // Act
        await repository.SaveAsync(promotion, CancellationToken.None);

        // Assert
        using var readDbContext = CreateDbContext();
        var readRepository = new PromotionRepository(readDbContext);
        var retrieved = await readRepository.GetByIdAsync(promotion.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(promotion.Id);
        retrieved.ApplicationId.Should().Be(appId);
        retrieved.Version.Should().Be(version);
        retrieved.TargetEnvironment.Should().Be(Domain.Enums.Environment.Dev);
        retrieved.Status.Should().Be(PromotionStatus.Pending);
        retrieved.RequestedBy.Should().Be(requestedBy);
        retrieved.RequestedAt.Should().BeCloseTo(promotion.RequestedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetActiveByApplication_excludes_terminal_states()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new PromotionRepository(dbContext);

        var appId = new ApplicationId(Guid.NewGuid());
        var version1 = new AppVersion("1.0.0");
        var version2 = new AppVersion("0.9.0");
        var userId = Guid.NewGuid();

        // 1. Pending (Active)
        var pending = Promotion.Request(appId, version1, Domain.Enums.Environment.Dev, userId, [], []);

        // 2. Completed (Terminal)
        var completed = Promotion.Request(appId, version2, Domain.Enums.Environment.Dev, userId, [], []);
        completed.Approve(userId, ["approver"]);
        completed.StartDeployment(userId);
        completed.Complete(userId);

        await repository.SaveAsync(pending, CancellationToken.None);
        await repository.SaveAsync(completed, CancellationToken.None);

        // Act
        using var readDbContext = CreateDbContext();
        var readRepository = new PromotionRepository(readDbContext);
        var activePromotions = await readRepository.GetActiveByApplicationAsync(appId, CancellationToken.None);

        // Assert
        activePromotions.Should().ContainSingle();
        activePromotions.First().Id.Should().Be(pending.Id);
    }

    [Fact]
    public async Task GetCompletedByApplication_returns_only_completed()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new PromotionRepository(dbContext);

        var appId = new ApplicationId(Guid.NewGuid());
        var userId = Guid.NewGuid();

        // 1. Pending
        var pending = Promotion.Request(appId, new AppVersion("1.0.0"), Domain.Enums.Environment.Dev, userId, [], []);

        // 2. InProgress
        var inProgress = Promotion.Request(appId, new AppVersion("1.1.0"), Domain.Enums.Environment.Dev, userId, [], []);
        inProgress.Approve(userId, ["approver"]);
        inProgress.StartDeployment(userId);

        // 3. Completed
        var completed = Promotion.Request(appId, new AppVersion("1.2.0"), Domain.Enums.Environment.Dev, userId, [], []);
        completed.Approve(userId, ["approver"]);
        completed.StartDeployment(userId);
        completed.Complete(userId);

        // 4. Cancelled
        var cancelled = Promotion.Request(appId, new AppVersion("1.3.0"), Domain.Enums.Environment.Dev, userId, [], []);
        cancelled.Cancel(userId);

        await repository.SaveAsync(pending, CancellationToken.None);
        await repository.SaveAsync(inProgress, CancellationToken.None);
        await repository.SaveAsync(completed, CancellationToken.None);
        await repository.SaveAsync(cancelled, CancellationToken.None);

        // Act
        using var readDbContext = CreateDbContext();
        var readRepository = new PromotionRepository(readDbContext);
        var completedPromotions = await readRepository.GetCompletedByApplicationAsync(appId, CancellationToken.None);

        // Assert
        completedPromotions.Should().ContainSingle();
        completedPromotions.First().Id.Should().Be(completed.Id);
    }

    [Fact]
    public async Task SaveAsync_persists_state_transitions()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new PromotionRepository(dbContext);

        var appId = new ApplicationId(Guid.NewGuid());
        var userId = Guid.NewGuid();
        var promotion = Promotion.Request(appId, new AppVersion("1.0.0"), Domain.Enums.Environment.Dev, userId, [], []);
        promotion.Approve(userId, ["approver"]);

        await repository.SaveAsync(promotion, CancellationToken.None);

        // Act
        using var readDbContext = CreateDbContext();
        var readRepository = new PromotionRepository(readDbContext);
        var retrieved = await readRepository.GetByIdAsync(promotion.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.StateHistory.Should().HaveCount(2);
        retrieved.StateHistory[0].Status.Should().Be(PromotionStatus.Pending);
        retrieved.StateHistory[1].Status.Should().Be(PromotionStatus.Approved);
    }

    [Fact]
    public async Task Reconstitute_preserves_domain_events_cleared()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var repository = new PromotionRepository(dbContext);

        var appId = new ApplicationId(Guid.NewGuid());
        var userId = Guid.NewGuid();
        var promotion = Promotion.Request(appId, new AppVersion("1.0.0"), Domain.Enums.Environment.Dev, userId, [], []);

        // Locally the promotion has the PromotionRequestedEvent raised
        promotion.DomainEvents.Should().NotBeEmpty();

        await repository.SaveAsync(promotion, CancellationToken.None);

        // Act
        using var readDbContext = CreateDbContext();
        var readRepository = new PromotionRepository(readDbContext);
        var retrieved = await readRepository.GetByIdAsync(promotion.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.DomainEvents.Should().BeEmpty();
    }
}
