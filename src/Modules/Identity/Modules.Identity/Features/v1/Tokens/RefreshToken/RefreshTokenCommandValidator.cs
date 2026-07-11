using FluentValidation;

using Modules.Identity.Contracts.v1.Tokens.RefreshToken;

namespace Modules.Identity.Features.v1.Tokens.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}