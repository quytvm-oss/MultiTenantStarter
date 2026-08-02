using FluentValidation;

using Modules.Identity.Contracts.v1.Impersonation.RevokeImpersonationGrant;

namespace Modules.Identity.Features.v1.Impersonation.RevokeImpersonationGrant;

public class RevokeImpersonationGrantCommandValidator : AbstractValidator<RevokeImpersonationGrantCommand>
{
    public RevokeImpersonationGrantCommandValidator()
    {
        RuleFor(p => p.GrantId)
            .NotEmpty();

        RuleFor(p => p.Reason)
            .MaximumLength(512)
            .When(p => !string.IsNullOrEmpty(p.Reason));
    }
}