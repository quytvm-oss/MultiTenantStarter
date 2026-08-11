using FluentValidation;

using Modules.Auditing.Contracts.v1.GetAuditsByCorrelation;

namespace Modules.Auditing.Features.GetAuditsByCorrelation;

public class GetAuditsByCorrelationQueryValidator : AbstractValidator<GetAuditsByCorrelationQuery>
{
    public GetAuditsByCorrelationQueryValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();
        
        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <=  q.ToUtc)
            .WithMessage("From and To are both neither from nor to");
    }
}