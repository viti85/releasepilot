using MediatR;

namespace ReleasePilot.Application.Commands;

public sealed record CancelPromotionCommand(
    Guid PromotionId,
    Guid UserId) : IRequest<Unit>;
