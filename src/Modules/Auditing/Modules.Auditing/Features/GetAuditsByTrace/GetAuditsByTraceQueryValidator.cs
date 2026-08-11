using FluentValidation;

using Modules.Auditing.Contracts.v1.GetAuditsByTrace;

namespace Modules.Auditing.Features.GetAuditsByTrace;

public class GetAuditsByTraceQueryValidator : AbstractValidator<GetAuditsByTraceQuery>
{
    public GetAuditsByTraceQueryValidator()
    {
        RuleFor(q => q.TraceId)
            .NotEmpty();
        
        RuleFor(q => q)
            .Must(q => !q.FromUtc.HasValue || !q.ToUtc.HasValue || q.FromUtc <=  q.ToUtc)
            .WithMessage("From and To are both neither from nor to");
    }
}