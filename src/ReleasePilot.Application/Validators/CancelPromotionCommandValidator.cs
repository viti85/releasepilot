using FluentValidation;
using ReleasePilot.Application.Commands;

namespace ReleasePilot.Application.Validators;

public sealed class CancelPromotionCommandValidator : AbstractValidator<CancelPromotionCommand>
{
    public CancelPromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
