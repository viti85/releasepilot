using FluentValidation;
using ReleasePilot.Application.Commands;

namespace ReleasePilot.Application.Validators;

public sealed class ApprovePromotionCommandValidator : AbstractValidator<ApprovePromotionCommand>
{
    public ApprovePromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
        RuleFor(x => x.ApproverId).NotEmpty();
        RuleFor(x => x.ApproverRoles).NotEmpty();
    }
}
