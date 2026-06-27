using MediatR;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Commands;
using ReleasePilot.Application.Exceptions;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Handlers;

public sealed class StartDeploymentHandler(
    IPromotionRepository repository,
    IEventBus eventBus,
    IDeploymentPort deploymentPort) : IRequestHandler<StartDeploymentCommand, Unit>
{
    public async Task<Unit> Handle(StartDeploymentCommand command, CancellationToken ct)
    {
        var id = new PromotionId(command.PromotionId);
        var promotion = await repository.GetByIdAsync(id, ct)
            ?? throw new PromotionNotFoundException(command.PromotionId);

        promotion.StartDeployment(command.UserId);

        await repository.SaveAsync(promotion, ct);
        await eventBus.PublishAsync(promotion.DomainEvents, ct);
        promotion.ClearDomainEvents();

        await deploymentPort.TriggerAsync(promotion.Id, ct);

        return Unit.Value;
    }
}
