using FluentValidation;

using Modules.Auditing.Contracts.v1.GetSecurityAudits;

namespace Modules.Auditing.Features.GetSecurityAudits;

public sealed class GetSecurityAuditsQueryValidator : AbstractValidator<GetSecurityAuditsQuery>
{
    public GetSecurityAuditsQueryValidator()
    {
        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <= q.ToUtc)
            .WithMessage("FromUtc must be less than or equal to ToUtc.");
    }
}