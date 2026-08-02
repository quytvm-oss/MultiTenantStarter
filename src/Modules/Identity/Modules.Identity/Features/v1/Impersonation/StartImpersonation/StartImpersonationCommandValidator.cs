using FluentValidation;

using Modules.Identity.Contracts.v1.Impersonation.StartImpersonation;

namespace Modules.Identity.Features.v1.Impersonation.StartImpersonation;

public class StartImpersonationCommandValidator : AbstractValidator<StartImpersonationCommand>
{
    /// <summary>
    /// Upper bound on impersonation token lifetime — the server will silently
    /// cap to this even if the validator passes, but we reject obvious abuse
    /// (negative, zero, or absurd values) up front.
    /// </summary>
    public const int MaxImpersonationMinutes = 60;
    
    public StartImpersonationCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
        
        RuleFor(x => x.TargetTenantId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
        
        RuleFor(x => x.DurationMinutes!.Value)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxImpersonationMinutes)
            .WithMessage($"Duration must be between 1 and {MaxImpersonationMinutes} minutes.")
            .When(x => x.DurationMinutes.HasValue)
            ;
    }
}