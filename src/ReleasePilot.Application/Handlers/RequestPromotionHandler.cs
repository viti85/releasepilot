using MediatR;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Commands;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Handlers;

public sealed class RequestPromotionHandler(
    IPromotionRepository repository,
    IEventBus eventBus) : IRequestHandler<RequestPromotionCommand, PromotionId>
{
    public async Task<PromotionId> Handle(RequestPromotionCommand command, CancellationToken ct)
    {
        var appId = new ApplicationId(command.ApplicationId);
        var version = new AppVersion(command.Version);

        var active = await repository.GetActiveByApplicationAsync(appId, ct);
        var completed = await repository.GetCompletedByApplicationAsync(appId, ct);

        var promotion = Promotion.Request(appId, version, command.TargetEnvironment, command.RequestedByUserId, active, completed);

        await repository.SaveAsync(promotion, ct);
        await eventBus.PublishAsync(promotion.DomainEvents, ct);
        promotion.ClearDomainEvents();

        return promotion.Id;
    }
}
