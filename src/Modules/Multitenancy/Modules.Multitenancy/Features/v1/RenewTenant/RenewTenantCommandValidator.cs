using FluentValidation;

using Modules.Multitenancy.Contracts.v1.RenewTenant;

namespace Modules.Multitenancy.Features.v1.RenewTenant;

/// <summary>
/// Renews a tenant for one more plan term. When <see cref="PlanKey"/> is null the current plan is
/// renewed; when it differs the tenant is switched to the new plan from the renewal forward.
/// </summary>
public class RenewTenantCommandValidator : AbstractValidator<RenewTenantCommand>
{
    public RenewTenantCommandValidator()
    {
        //RuleFor(x => x.)
    }
}