using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.ValueObjects;
using ReleasePilot.Infrastructure.Persistence;
using ReleasePilot.Tests.Api.Infrastructure;
using Xunit;

namespace ReleasePilot.Tests.Api.Middleware;

public class DomainExceptionMiddlewareTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;
    private readonly HttpClient _client;

    public DomainExceptionMiddlewareTests(WebApiFactory factory)
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
    public async Task DomainException_MapsTo422UnprocessableEntity()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange: Request a Production promotion directly without completing Dev/Staging first
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
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWrapper>();
        problem.Should().NotBeNull();
        problem!.Type.Should().Contain("environment-skipped");
        problem.Title.Should().Be("Environment skipped");
        problem.Status.Should().Be(422);
        problem.Detail.Should().Contain("Staging");
    }

    [Fact]
    public async Task ConcurrentPromotionException_MapsTo409Conflict()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange: Create first active promotion for application on Dev
        var appId = Guid.NewGuid();
        var request = new
        {
            applicationId = appId,
            version = "1.0.0",
            targetEnvironment = "Dev",
            requestedByUserId = Guid.NewGuid()
        };
        var createResponse1 = await _client.PostAsJsonAsync("/promotions", request);
        createResponse1.EnsureSuccessStatusCode();

        // Act: Request a second active promotion for same application on Dev
        var response = await _client.PostAsJsonAsync("/promotions", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWrapper>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Concurrent promotion");
        problem.Status.Should().Be(409);
    }

    [Fact]
    public async Task ImmutablePromotionException_MapsTo409Conflict()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange: Complete a promotion first
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

        // Approve, Start, Complete
        await _client.PostAsJsonAsync($"/promotions/{id}/approve", new { approverId = userId, approverRoles = new[] { "approver" } });
        await _client.PostAsJsonAsync($"/promotions/{id}/start", new { userId });
        await _client.PostAsJsonAsync($"/promotions/{id}/complete", new { userId });

        // Act: Try to approve the completed promotion again
        var approveRequest = new
        {
            approverId = userId,
            approverRoles = new[] { "approver" }
        };
        var response = await _client.PostAsJsonAsync($"/promotions/{id}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWrapper>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Immutable promotion");
        problem.Status.Should().Be(409);
    }

    [Fact]
    public async Task ValidationException_MapsTo400BadRequest()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange: Missing applicationId
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
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsValidationWrapper>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Validation error");
        problem.Status.Should().Be(400);
        problem.Errors.Should().NotBeNull();
        problem.Errors.Should().ContainKey("ApplicationId");
    }

    [Fact]
    public async Task UnauthorizedApprovalException_MapsTo403Forbidden()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange: Create promotion first
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

        // Act: Try to approve with empty roles
        var approveRequest = new
        {
            approverId = Guid.NewGuid(),
            approverRoles = Array.Empty<string>()
        };
        var response = await _client.PostAsJsonAsync($"/promotions/{id}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWrapper>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Unauthorized approval");
        problem.Status.Should().Be(403);
    }

    [Fact]
    public async Task UnknownException_MapsTo500InternalServerErrorWithoutStackTrace()
    {
        if (!_factory.DockerAvailable) return;

        // Arrange: Inject a broken IPromotionRepository using WithWebHostBuilder
        var brokenFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var mockRepository = Substitute.For<IPromotionRepository>();
                mockRepository.GetActiveByApplicationAsync(Arg.Any<ReleasePilot.Domain.ValueObjects.ApplicationId>(), Arg.Any<CancellationToken>())
                    .ThrowsAsync(new Exception("Database connection lost permanently!"));
                
                // Replace registered repository
                services.AddScoped(_ => mockRepository);
            });
        });
        var brokenClient = brokenFactory.CreateClient();

        var request = new
        {
            applicationId = Guid.NewGuid(),
            version = "1.0.0",
            targetEnvironment = "Dev",
            requestedByUserId = Guid.NewGuid()
        };

        // Act
        var response = await brokenClient.PostAsJsonAsync("/promotions", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        var bodyText = await response.Content.ReadAsStringAsync();
        bodyText.Should().NotContain("StackTrace");
        bodyText.Should().NotContain("Exception");
        bodyText.Should().NotContain("Database connection lost permanently!");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWrapper>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Internal server error");
        problem.Detail.Should().Be("An unexpected error occurred.");
        problem.Status.Should().Be(500);
    }

    private record ResponseIdWrapper(Guid Id);
    private record ProblemDetailsWrapper(string Type, string Title, int Status, string Detail, string Instance);
    private record ProblemDetailsValidationWrapper(string Type, string Title, int Status, string Detail, string Instance, Dictionary<string, string[]> Errors);
}
