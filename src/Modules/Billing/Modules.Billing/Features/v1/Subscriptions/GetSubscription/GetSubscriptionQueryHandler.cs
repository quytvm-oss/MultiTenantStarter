using System;
using System.Collections.Generic;
using System.Text;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.Dtos;
using Modules.Billing.Contracts.v1.Subscriptions;
using Modules.Billing.Data;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Subscriptions.GetSubscription;

public class GetSubscriptionQueryHandler : IQueryHandler<GetSubscriptionQuery, SubscriptionDto?>
{
    private readonly BillingDbContext _dbContext;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public GetSubscriptionQueryHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    {
        _dbContext = dbContext;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }


    public async ValueTask<SubscriptionDto?> Handle(GetSubscriptionQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var callerTenantId = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedAccessException("Caller tenant ID is not available");

        // BillingDbContext is not tenant-filtered, so a tenant caller is pinned to its OWN
        // subscription and only root may pass an arbitrary tenant id (else cross-tenant reads).
        var tenantId = callerTenantId == MultitenancyConstants.Root.Id
            ? query.TenantId ?? callerTenantId
            : callerTenantId;

        var sub = await (from s in _dbContext.Subscriptions.AsNoTracking()
                        join p in _dbContext.Plans.AsNoTracking() on s.PlanId equals p.Id
                        where s.TenantId == tenantId && s.Status == Contracts.SubscriptionStatus.Active
                        select new SubscriptionDto(
                            s.Id,
                            s.TenantId,
                             s.PlanId,
                            p.Key,
                            s.StartUtc,
                            s.EndUtc,
                            s.Status
                        )).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return sub;
    }
}
