using MediatR;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Commands;
using ReleasePilot.Application.Exceptions;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Handlers;

public sealed class ApprovePromotionHandler(
    IPromotionRepository repository,
    IEventBus eventBus) : IRequestHandler<ApprovePromotionCommand, Unit>
{
    public async Task<Unit> Handle(ApprovePromotionCommand command, CancellationToken ct)
    {
        var id = new PromotionId(command.PromotionId);
        var promotion = await repository.GetByIdAsync(id, ct)
            ?? throw new PromotionNotFoundException(command.PromotionId);

        promotion.Approve(command.ApproverId, command.ApproverRoles);

        await repository.SaveAsync(promotion, ct);
        await eventBus.PublishAsync(promotion.DomainEvents, ct);
        promotion.ClearDomainEvents();

        return Unit.Value;
    }
}
