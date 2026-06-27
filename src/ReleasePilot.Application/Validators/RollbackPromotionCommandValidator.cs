using FluentValidation;
using ReleasePilot.Application.Commands;

namespace ReleasePilot.Application.Validators;

public sealed class RollbackPromotionCommandValidator : AbstractValidator<RollbackPromotionCommand>
{
    public RollbackPromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
    }
}
