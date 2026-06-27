using MediatR;
using ReleasePilot.Application.Dtos;

namespace ReleasePilot.Application.Queries;

public sealed record GetPromotionByIdQuery(Guid PromotionId) : IRequest<PromotionDetailDto?>;
