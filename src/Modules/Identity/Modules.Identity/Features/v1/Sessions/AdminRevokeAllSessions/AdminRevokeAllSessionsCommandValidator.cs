using FluentValidation;

using Modules.Identity.Contracts.v1.Sessions.AdminRevokeAllSessions;

namespace Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;

public class AdminRevokeAllSessionsCommandValidator : AbstractValidator<AdminRevokeAllSessionsCommand>
{
    public AdminRevokeAllSessionsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
        
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason is required and should be less than 500 characters")
            .When( x => x.Reason is not null);
    }
}