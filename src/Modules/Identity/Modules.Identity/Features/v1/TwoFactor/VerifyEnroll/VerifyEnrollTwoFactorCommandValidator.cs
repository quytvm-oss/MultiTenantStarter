using FluentValidation;

using Modules.Identity.Contracts.v1.TwoFactor;

namespace Modules.Identity.Features.v1.TwoFactor.VerifyEnroll;

public class VerifyEnrollTwoFactorCommandValidator : AbstractValidator<VerifyEnrollTwoFactorCommand>
{
    public VerifyEnrollTwoFactorCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(10);
    }
}