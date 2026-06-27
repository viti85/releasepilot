using MediatR;

namespace ReleasePilot.Application.Commands;

public sealed record ApprovePromotionCommand(
    Guid PromotionId,
    Guid ApproverId,
    IReadOnlyCollection<string> ApproverRoles) : IRequest<Unit>;
