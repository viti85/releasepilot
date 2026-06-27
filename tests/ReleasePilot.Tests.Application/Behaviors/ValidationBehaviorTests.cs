using FluentAssertions;
using FluentValidation;
using MediatR;
using ReleasePilot.Application.Behaviors;
using ReleasePilot.Application.Commands;
using ReleasePilot.Application.Validators;
using ReleasePilot.Domain.ValueObjects;
using Environment = ReleasePilot.Domain.Enums.Environment;

namespace ReleasePilot.Tests.Application.Behaviors;

public class ValidationBehaviorTests
{
    private static ValidationBehavior<RequestPromotionCommand, PromotionId> BuildBehavior() =>
        new([new RequestPromotionCommandValidator()]);

    private static RequestPromotionCommand ValidCommand() =>
        new(Guid.NewGuid(), "1.0.0", Environment.Dev, Guid.NewGuid());

    [Fact]
    public async Task Handle_WhenCommandHasEmptyApplicationId_ThrowsValidationExceptionBeforeHandler()
    {
        var behavior = BuildBehavior();
        var handlerCalled = 0;
        RequestHandlerDelegate<PromotionId> next = () =>
        {
            handlerCalled++;
            return Task.FromResult(PromotionId.New());
        };

        var invalidCommand = new RequestPromotionCommand(
            Guid.Empty, "1.0.0", Environment.Dev, Guid.NewGuid());

        var act = () => behavior.Handle(invalidCommand, next, CancellationToken.None);

        await act.Should().ThrowExactlyAsync<ValidationException>();
        handlerCalled.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_CallsHandlerExactlyOnce()
    {
        var behavior = BuildBehavior();
        var handlerCalled = 0;
        RequestHandlerDelegate<PromotionId> next = () =>
        {
            handlerCalled++;
            return Task.FromResult(PromotionId.New());
        };

        await behavior.Handle(ValidCommand(), next, CancellationToken.None);

        handlerCalled.Should().Be(1);
    }
}
