using System;
using System.Collections.Generic;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Modules.Billing.Contracts.v1.Invoices;
using Modules.Billing.Services;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Invoices.GenerateInvoices;

public sealed class GenerateInvoicesCommandHandler : ICommandHandler<GenerateInvoicesCommand, int>
{
    private readonly IBillingService _billing;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;

    public GenerateInvoicesCommandHandler(IBillingService billing, IMultiTenantContextAccessor<AppTenantInfo> accessor)
    {
        _billing = billing;
        _accessor = accessor;
    }

    public async ValueTask<int> Handle(GenerateInvoicesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Platform-wide invoice generation runs across EVERY tenant — it is a root-operator action.
        // A tenant admin (who also holds Billing.Manage) must not be able to trigger it.
        var callerTenantId = _accessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant is required.");
        if (callerTenantId != MultitenancyConstants.Root.Id)
        {
            throw new ForbiddenException("Only the root operator may generate invoices across tenants.");
        }

        return await _billing.GenerateInvoicesForAllTenantsAsync(command.PeriodYear, command.PeriodMonth, cancellationToken)
            .ConfigureAwait(false);
    }
}
