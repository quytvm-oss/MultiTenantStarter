using FluentValidation;

using Modules.Identity.Contracts.v1.Users.ForgotPassword;

namespace Modules.Identity.Features.v1.Users.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(p => p.Email).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();
    }
}