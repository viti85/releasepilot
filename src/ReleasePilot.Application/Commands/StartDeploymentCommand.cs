using MediatR;

namespace ReleasePilot.Application.Commands;

public sealed record StartDeploymentCommand(
    Guid PromotionId,
    Guid UserId) : IRequest<Unit>;
