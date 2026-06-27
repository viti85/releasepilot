using MediatR;

namespace ReleasePilot.Application.Commands;

public sealed record CompletePromotionCommand(
    Guid PromotionId,
    Guid UserId) : IRequest<Unit>;
