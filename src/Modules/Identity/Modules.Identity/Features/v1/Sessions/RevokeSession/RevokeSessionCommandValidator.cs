using FluentValidation;

using Modules.Identity.Contracts.v1.Sessions.RevokeSession;

namespace Modules.Identity.Features.v1.Sessions.RevokeSession;

public class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required");
    }
}