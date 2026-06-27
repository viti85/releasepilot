using MediatR;
using ReleasePilot.Application.Abstractions;
using ReleasePilot.Application.Dtos;
using ReleasePilot.Application.Queries;
using ReleasePilot.Domain.Aggregates;

namespace ReleasePilot.Application.Handlers;

public sealed class GetPromotionHistoryHandler(
    IPromotionRepository repository) : IRequestHandler<GetPromotionHistoryQuery, PagedResult<PromotionSummaryDto>>
{
    public async Task<PagedResult<PromotionSummaryDto>> Handle(GetPromotionHistoryQuery query, CancellationToken ct)
    {
        var appId = new ApplicationId(query.ApplicationId);
        var (items, total) = await repository.GetPagedByApplicationAsync(appId, query.Page, query.PageSize, ct);

        return new PagedResult<PromotionSummaryDto>(
            items.Select(ToSummary).ToList(),
            query.Page,
            query.PageSize,
            total);
    }

    private static PromotionSummaryDto ToSummary(Promotion p) =>
        new(
            p.Id.Value,
            p.Version.Value,
            p.TargetEnvironment.ToString(),
            p.Status.ToString(),
            p.RequestedAt);
}
