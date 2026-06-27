using FluentValidation;
using ReleasePilot.Application.Commands;

namespace ReleasePilot.Application.Validators;

public sealed class RequestPromotionCommandValidator : AbstractValidator<RequestPromotionCommand>
{
    public RequestPromotionCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Version).NotEmpty();
        RuleFor(x => x.TargetEnvironment).IsInEnum();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}
