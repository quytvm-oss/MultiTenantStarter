using FluentValidation;

using Modules.Identity.Contracts.v1.TwoFactor;

namespace Modules.Identity.Features.v1.TwoFactor.Disable;

public class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
{
    public DisableTwoFactorCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}