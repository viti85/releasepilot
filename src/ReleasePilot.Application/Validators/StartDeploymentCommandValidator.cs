using FluentValidation;
using ReleasePilot.Application.Commands;

namespace ReleasePilot.Application.Validators;

public sealed class StartDeploymentCommandValidator : AbstractValidator<StartDeploymentCommand>
{
    public StartDeploymentCommandValidator()
    {
        RuleFor(x => x.PromotionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
