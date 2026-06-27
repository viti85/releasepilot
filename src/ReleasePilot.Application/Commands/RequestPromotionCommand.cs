using MediatR;
using ReleasePilot.Domain.ValueObjects;

namespace ReleasePilot.Application.Commands;

public sealed record RequestPromotionCommand(
    Guid ApplicationId,
    string Version,
    Environment TargetEnvironment,
    Guid RequestedByUserId) : IRequest<PromotionId>;
