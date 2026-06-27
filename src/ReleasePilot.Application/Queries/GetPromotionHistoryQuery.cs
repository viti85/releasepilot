using MediatR;
using ReleasePilot.Application.Dtos;

namespace ReleasePilot.Application.Queries;

public sealed record GetPromotionHistoryQuery(
    Guid ApplicationId,
    int Page,
    int PageSize) : IRequest<PagedResult<PromotionSummaryDto>>;
