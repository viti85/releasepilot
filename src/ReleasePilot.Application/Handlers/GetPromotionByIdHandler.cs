using MediatR;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Dtos;
using ReleasePilot.Application.Queries;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Handlers;

public sealed class GetPromotionByIdHandler(
    IPromotionRepository repository) : IRequestHandler<GetPromotionByIdQuery, PromotionDetailDto?>
{
    public async Task<PromotionDetailDto?> Handle(GetPromotionByIdQuery query, CancellationToken ct)
    {
        var id = new PromotionId(query.PromotionId);
        var promotion = await repository.GetByIdAsync(id, ct);
        return promotion is null ? null : ToDetail(promotion);
    }

    private static PromotionDetailDto ToDetail(Promotion p) =>
        new(
            p.Id.Value,
            p.ApplicationId.Value,
            p.Version.Value,
            p.TargetEnvironment.ToString(),
            p.Status.ToString(),
            p.RequestedBy,
            p.ApprovedBy,
            p.RequestedAt,
            p.CompletedAt,
            p.StateHistory
                .Select(s => new StateTransitionDto(s.Status.ToString(), s.Timestamp, s.UserId))
                .ToList());
}
