using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Tests.Api.Infrastructure;
using Xunit;

namespace ReleasePilot.Tests.Api.Endpoints;

public class PromotionQueryEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;
    private readonly HttpClient _client;

    public PromotionQueryEndpointTests(WebApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        if (_factory.DockerAvailable)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ReleasePilotDbContext>();
            dbContext.Database.ExecuteSqlRaw("TRUNCATE TABLE promotion_state_transitions, promotions, audit_log CASCADE;");
        }
    }

    [Fact]
    public async Task GetPromotionById_ExistingPromotion_Returns200OkWithDetails()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var request = new
        {
            applicationId = Guid.NewGuid(),
            version = "1.0.0",
            targetEnvironment = "Dev",
            requestedByUserId = Guid.NewGuid()
        };
        var createResponse = await _client.PostAsJsonAsync("/promotions", request);
        createResponse.EnsureSuccessStatusCode();
        var id = (await createResponse.Content.ReadFromJsonAsync<ResponseIdWrapper>())!.Id;

        // Act
        var response = await _client.GetAsync($"/promotions/{id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PromotionDetailWrapper>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(id);
        body.Status.Should().Be("Pending");
        body.TargetEnvironment.Should().Be("Dev");
        body.Version.Should().Be("1.0.0");
        body.StateHistory.Should().NotBeEmpty();
        body.StateHistory[0].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetPromotionById_NotFound_Returns404NotFoundWithProblemDetails()
    {
        if (!_factory.DockerAvailable) return;

        // Act
        var response = await _client.GetAsync($"/promotions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDetailWrapper>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Not found");
    }

    [Fact]
    public async Task GetApplicationStatus_NoPromotions_Returns200OkWithNullEnvironments()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var appId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/applications/{appId}/status");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationStatusWrapper>();
        body.Should().NotBeNull();
        body!.ApplicationId.Should().Be(appId);
        body.Environments.Should().ContainKey("Dev");
        body.Environments.Should().ContainKey("Staging");
        body.Environments.Should().ContainKey("Production");
        body.Environments["Dev"].LastCompletedVersion.Should().BeNull();
        body.Environments["Dev"].ActivePromotion.Should().BeNull();
        body.Environments["Staging"].LastCompletedVersion.Should().BeNull();
        body.Environments["Staging"].ActivePromotion.Should().BeNull();
        body.Environments["Production"].LastCompletedVersion.Should().BeNull();
        body.Environments["Production"].ActivePromotion.Should().BeNull();
    }

    [Fact]
    public async Task GetApplicationStatus_WithActivePromotion_ReturnsActivePromotionForTargetEnvironment()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var appId = Guid.NewGuid();
        var request = new
        {
            applicationId = appId,
            version = "1.0.0",
            targetEnvironment = "Dev",
            requestedByUserId = Guid.NewGuid()
        };
        var createResponse = await _client.PostAsJsonAsync("/promotions", request);
        createResponse.EnsureSuccessStatusCode();
        var promoId = (await createResponse.Content.ReadFromJsonAsync<ResponseIdWrapper>())!.Id;

        // Act
        var response = await _client.GetAsync($"/applications/{appId}/status");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationStatusWrapper>();
        body.Should().NotBeNull();
        body!.Environments["Dev"].ActivePromotion.Should().NotBeNull();
        body.Environments["Dev"].ActivePromotion!.Id.Should().Be(promoId);
        body.Environments["Staging"].ActivePromotion.Should().BeNull();
        body.Environments["Production"].ActivePromotion.Should().BeNull();
    }

    [Fact]
    public async Task GetPromotionHistory_Pagination_ReturnsPaginatedResults()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Create and cancel 3 promotions sequentially to avoid concurrency blocks
        for (int i = 1; i <= 3; i++)
        {
            var request = new
            {
                applicationId = appId,
                version = $"1.0.{i}",
                targetEnvironment = "Dev",
                requestedByUserId = userId
            };
            var createRes = await _client.PostAsJsonAsync("/promotions", request);
            createRes.EnsureSuccessStatusCode();
            var id = (await createRes.Content.ReadFromJsonAsync<ResponseIdWrapper>())!.Id;

            // Cancel it
            var cancelRes = await _client.PostAsJsonAsync($"/promotions/{id}/cancel", new { userId });
            cancelRes.EnsureSuccessStatusCode();
        }

        // Act
        var response = await _client.GetAsync($"/applications/{appId}/promotions?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponseWrapper<PromotionSummaryWrapper>>();
        body.Should().NotBeNull();
        body!.Items.Count.Should().Be(2);
        body.TotalCount.Should().Be(3);
        body.TotalPages.Should().Be(2);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetPromotionHistory_EmptyHistory_ReturnsEmptyPaginatedResponse()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var appId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/applications/{appId}/promotions");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponseWrapper<PromotionSummaryWrapper>>();
        body.Should().NotBeNull();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
        body.Page.Should().Be(1);
    }

    private record ResponseIdWrapper(Guid Id);
    private record StateTransitionWrapper(string Status, DateTime Timestamp, Guid? UserId);
    private record PromotionDetailWrapper(
        Guid Id,
        Guid ApplicationId,
        string Version,
        string TargetEnvironment,
        string Status,
        Guid RequestedBy,
        Guid? ApprovedBy,
        DateTime RequestedAt,
        DateTime? CompletedAt,
        IReadOnlyList<StateTransitionWrapper> StateHistory);

    private record ProblemDetailsDetailWrapper(string Title, int Status, string Detail);
    private record EnvironmentStatusWrapper(string? LastCompletedVersion, PromotionSummaryWrapper? ActivePromotion);
    private record ApplicationStatusWrapper(Guid ApplicationId, IReadOnlyDictionary<string, EnvironmentStatusWrapper> Environments);
    private record PromotionSummaryWrapper(Guid Id, string Version, string TargetEnvironment, string Status, DateTime RequestedAt);
    private record PagedResponseWrapper<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
}
