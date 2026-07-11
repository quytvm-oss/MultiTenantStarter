using FluentValidation;

using Modules.Identity.Contracts.v1.Tokens.TokenGeneration;

namespace Modules.Identity.Features.v1.Tokens.TokenGeneration;

public class GenerateTokenCommandValidator : AbstractValidator<GenerateTokenCommand>
{
    public GenerateTokenCommandValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();
        
        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}