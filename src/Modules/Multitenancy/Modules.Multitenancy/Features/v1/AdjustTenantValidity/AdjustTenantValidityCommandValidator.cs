using FluentValidation;

using Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Features.v1.AdjustTenantValidity;

public class AdjustTenantValidityCommandValidator : AbstractValidator<AdjustTenantValidityCommand>
{
    public AdjustTenantValidityCommandValidator()
    {
        RuleFor(t => t.TenantId).NotEmpty();

        // The root operator tenant must never expire — block adjusting its validity (mirrors the
        // Activate/Deactivate guards that already refuse the root tenant).
        RuleFor(t => t.TenantId)
            .Must(id => !string.Equals(id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            .WithMessage("The root operator tenant's validity cannot be adjusted.");

        RuleFor(t => t.ValidUpto)
            .Must(d => d != default)
            .WithMessage("A valid 'validUpto' date is required.");
    }
}