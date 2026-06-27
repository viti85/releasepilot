using FluentValidation;
using ReleasePilot.Application.Commands;

namespace ReleasePilot.Application.Validators;

public sealed class CompletePromotionCommandValidator : AbstractValidator<CompletePromotionCommand>
{
    public CompletePromotionCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
