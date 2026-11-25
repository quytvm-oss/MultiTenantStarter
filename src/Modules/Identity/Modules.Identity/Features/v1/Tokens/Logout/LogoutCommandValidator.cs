using FluentValidation;

using Modules.Identity.Contracts.v1.Tokens.Logout;

namespace Modules.Identity.Features.v1.Tokens.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}