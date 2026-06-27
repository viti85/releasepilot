using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Commands;
using ReleasePilot.Application.Handlers;
using ReleasePilot.Domain.Abstractions;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.Events;
using ReleasePilot.Domain.Exceptions;
using ReleasePilot.Domain.ValueObjects;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;
using Environment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Tests.Application.Handlers;

public class RequestPromotionHandlerTests
{
    private readonly IPromotionRepository _repository = Substitute.For<IPromotionRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly RequestPromotionHandler _handler;

    public RequestPromotionHandlerTests()
    {
        _handler = new RequestPromotionHandler(_repository, _eventBus);

        _repository.GetActiveByApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Promotion>>([]));
        _repository.GetCompletedByApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Promotion>>([]));
    }

    [Fact]
    public async Task Handle_HappyPath_SavesPromotionInPendingStatus()
    {
        var command = new RequestPromotionCommand(Guid.NewGuid(), "1.0.0", Environment.Dev, Guid.NewGuid());

        await _handler.Handle(command, CancellationToken.None);

        await _repository.Received(1).SaveAsync(
            Arg.Is<Promotion>(p => p.Status == PromotionStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_PublishesPromotionRequestedEvent()
    {
        IReadOnlyCollection<IDomainEvent>? captured = null;
        _eventBus
            .PublishAsync(
                Arg.Do<IReadOnlyCollection<IDomainEvent>>(e => captured = e.ToList()),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = new RequestPromotionCommand(Guid.NewGuid(), "1.0.0", Environment.Dev, Guid.NewGuid());
        await _handler.Handle(command, CancellationToken.None);

        captured.Should().ContainSingle(e => e is PromotionRequestedEvent);
    }

    [Fact]
    public async Task Handle_WhenTargetIsStagingWithNoCompletedDev_ThrowsEnvironmentSkippedException()
    {
        var command = new RequestPromotionCommand(Guid.NewGuid(), "1.0.0", Environment.Staging, Guid.NewGuid());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowExactlyAsync<EnvironmentSkippedException>();
    }

    [Fact]
    public async Task Handle_WhenActivePromotionExistsForSameAppAndEnv_ThrowsConcurrentPromotionException()
    {
        var appId = Guid.NewGuid();
        var existing = Promotion.Request(
            new ApplicationId(appId), new AppVersion("0.9.0"), Environment.Dev, Guid.NewGuid(), [], []);

        _repository.GetActiveByApplicationAsync(Arg.Any<ApplicationId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Promotion>>([existing]));

        var command = new RequestPromotionCommand(appId, "1.0.0", Environment.Dev, Guid.NewGuid());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowExactlyAsync<ConcurrentPromotionException>();
    }
}
