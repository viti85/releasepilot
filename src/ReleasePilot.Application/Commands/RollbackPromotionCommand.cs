using MediatR;

namespace ReleasePilot.Application.Commands;

public sealed record RollbackPromotionCommand(
    Guid PromotionId,
    Guid UserId,
    string Reason) : IRequest<Unit>;
