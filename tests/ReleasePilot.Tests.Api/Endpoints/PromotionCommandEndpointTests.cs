using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Tests.Api.Infrastructure;
using Xunit;

namespace ReleasePilot.Tests.Api.Endpoints;

public class PromotionCommandEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;
    private readonly HttpClient _client;

    public PromotionCommandEndpointTests(WebApiFactory factory)
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
    public async Task RequestPromotion_WithValidBody_Returns201Created()
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

        // Act
        var response = await _client.PostAsJsonAsync("/promotions", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/promotions/");

        var body = await response.Content.ReadFromJsonAsync<ResponseIdWrapper>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ApprovePromotion_WithAuthorizedUser_Returns200Ok()
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
        var createResult = await createResponse.Content.ReadFromJsonAsync<ResponseIdWrapper>();
        var id = createResult!.Id;

        // Act
        var approveRequest = new
        {
            approverId = Guid.NewGuid(),
            approverRoles = new[] { "approver" }
        };
        var response = await _client.PostAsJsonAsync($"/promotions/{id}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<StatusResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task RequestPromotion_WithEmptyApplicationId_Returns400BadRequest()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var request = new
        {
            applicationId = Guid.Empty,
            version = "1.0.0",
            targetEnvironment = "Dev",
            requestedByUserId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/promotions", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWrapper>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Contain("validation");
        problem.Errors.Should().NotBeNull();
        problem.Errors.Should().ContainKey("ApplicationId");
    }

    [Fact]
    public async Task RequestPromotion_WithEnvironmentSkipped_Returns422UnprocessableEntity()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var request = new
        {
            applicationId = Guid.NewGuid(),
            version = "1.0.0",
            targetEnvironment = "Production",
            requestedByUserId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/promotions", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDetailWrapper>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("Staging");
    }

    [Fact]
    public async Task ApprovePromotion_WithNonApproverUser_Returns403Forbidden()
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
        var createResult = await createResponse.Content.ReadFromJsonAsync<ResponseIdWrapper>();
        var id = createResult!.Id;

        // Act
        var approveRequest = new
        {
            approverId = Guid.NewGuid(),
            approverRoles = Array.Empty<string>()
        };
        var response = await _client.PostAsJsonAsync($"/promotions/{id}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelPromotion_WhenAlreadyCompleted_Returns409Conflict()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new
        {
            applicationId = appId,
            version = "1.0.0",
            targetEnvironment = "Dev",
            requestedByUserId = userId
        };
        var createRes = await _client.PostAsJsonAsync("/promotions", request);
        var id = (await createRes.Content.ReadFromJsonAsync<ResponseIdWrapper>())!.Id;

        // Approve
        await _client.PostAsJsonAsync($"/promotions/{id}/approve", new { approverId = userId, approverRoles = new[] { "approver" } });
        
        // Start
        await _client.PostAsJsonAsync($"/promotions/{id}/start", new { userId });

        // Complete
        await _client.PostAsJsonAsync($"/promotions/{id}/complete", new { userId });

        // Act
        var response = await _client.PostAsJsonAsync($"/promotions/{id}/cancel", new { userId });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDetailWrapper>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("Completed");
    }

    private record ResponseIdWrapper(Guid Id);
    private record StatusResponse(Guid Id, string Status);
    private record ProblemDetailsWrapper(string Title, int Status, Dictionary<string, string[]> Errors);
    private record ProblemDetailsDetailWrapper(string Title, int Status, string Detail);
}
