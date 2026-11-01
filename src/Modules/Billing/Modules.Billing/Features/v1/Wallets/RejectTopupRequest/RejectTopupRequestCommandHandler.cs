using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts;
using Modules.Billing.Contracts.v1.Wallets;
using Modules.Billing.Data;
using Modules.Billing.Services;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Wallets.RejectTopupRequest;

public sealed class RejectTopupRequestCommandHandler : ICommandHandler<RejectTopupRequestCommand, Guid>
{
    private readonly BillingDbContext _dbContext;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

    private readonly IBillingService _billing;

    public RejectTopupRequestCommandHandler(
        BillingDbContext dbContext, 
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor, 
        IBillingService billing)
    {
        _dbContext = dbContext;
        _tenantAccessor = tenantAccessor;
        _billing = billing;
    }

    public async ValueTask<Guid> Handle(RejectTopupRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant is required.");
        var isRoot = tenantId == MultitenancyConstants.Root.Id;

        var request = await _dbContext.TopupRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Top-up request {command.Id} not found.");

        if (!isRoot && request.TenantId != tenantId)
        {
            throw new UnauthorizedException("You can only reject top-up requests for your own tenant.");
        }

        if (request.Status != TopupRequestStatus.Pending)
        {
            throw new CustomException(
                $"Top-up request {command.Id} cannot be rejected because it is {request.Status} (only Pending requests can be rejected).",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        request.Reject(command.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return request.Id;
    }
}
