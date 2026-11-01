using System;
using System.Collections.Generic;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.v1.Wallets;
using Modules.Billing.Data;
using Modules.Billing.Services;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Wallets.ApproveTopupRequest;

public sealed class ApproveTopupRequestCommandHandler : ICommandHandler<ApproveTopupRequestCommand, Guid>
{
    private readonly BillingDbContext _dbContext;
    private readonly IBillingService _billing;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    public ApproveTopupRequestCommandHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor, IBillingService billing)
    {
        _dbContext = dbContext;
        _tenantAccessor = tenantAccessor;
        _billing = billing;
    }

    public async ValueTask<Guid> Handle(ApproveTopupRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant is required.");
        var isRoot = tenantId == MultitenancyConstants.Root.Id;

        var request = await _dbContext.TopupRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Top-up request {command.Id} not found.");

        if (!isRoot && request.TenantId != tenantId)
        {
            throw new UnauthorizedException("You can only approve top-up requests for your own tenant.");
        }

        // For root, operate on the request's own tenant; for non-root, callerTenantId equals request.TenantId.
        var invoice = await _billing.CreateTopupInvoiceAsync(tenantId, request.Id, cancellationToken)
            .ConfigureAwait(false);

        return invoice.Id;
    }
}
