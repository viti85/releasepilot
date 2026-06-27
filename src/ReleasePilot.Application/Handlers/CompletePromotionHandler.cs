using MediatR;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Commands;
using ReleasePilot.Application.Exceptions;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Handlers;

public sealed class CompletePromotionHandler(
    IPromotionRepository repository,
    IEventBus eventBus,
    INotificationPort notificationPort) : IRequestHandler<CompletePromotionCommand, Unit>
{
    public async Task<Unit> Handle(CompletePromotionCommand command, CancellationToken ct)
    {
        var id = new PromotionId(command.PromotionId);
        var promotion = await repository.GetByIdAsync(id, ct)
            ?? throw new PromotionNotFoundException(command.PromotionId);

        promotion.Complete(command.UserId);

        await repository.SaveAsync(promotion, ct);
        await eventBus.PublishAsync(promotion.DomainEvents, ct);
        promotion.ClearDomainEvents();

        await notificationPort.NotifyTerminalStateAsync(promotion.Id, promotion.Status, ct);

        return Unit.Value;
    }
}
