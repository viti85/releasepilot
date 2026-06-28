using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Commands;
using ReleasePilot.Application.Exceptions;
using ReleasePilot.Application.Handlers;
using ReleasePilot.Domain.Abstractions;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.Exceptions;
using ReleasePilot.Domain.ValueObjects;
using ApplicationId = ReleasePilot.Domain.ValueObjects.ApplicationId;
using Environment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Tests.Application.Handlers;

public class ApprovePromotionHandlerTests
{
    private readonly IPromotionRepository _repository = Substitute.For<IPromotionRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ApprovePromotionHandler _handler;

    public ApprovePromotionHandlerTests()
    {
        _handler = new ApprovePromotionHandler(_repository, _eventBus);
    }

    private static Promotion CreatePendingDevPromotion() =>
        Promotion.Request(ApplicationId.New(), new AppVersion("1.0.0"), Environment.Dev, Guid.NewGuid(), [], []);

    [Fact]
    public async Task Handle_HappyPath_TransitionsToApprovedAndSavesAndPublishes()
    {
        var pending = CreatePendingDevPromotion();
        _repository.GetByIdAsync(Arg.Any<PromotionId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Promotion?>(pending));

        var command = new ApprovePromotionCommand(Guid.NewGuid(), Guid.NewGuid());

        await _handler.Handle(command, CancellationToken.None);

        pending.Status.Should().Be(PromotionStatus.Approved);
        await _repository.Received(1).SaveAsync(pending, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Any<IReadOnlyCollection<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPromotionNotFound_ThrowsPromotionNotFoundException()
    {
        _repository.GetByIdAsync(Arg.Any<PromotionId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Promotion?>(null));

        var command = new ApprovePromotionCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowExactlyAsync<PromotionNotFoundException>();
    }

}
