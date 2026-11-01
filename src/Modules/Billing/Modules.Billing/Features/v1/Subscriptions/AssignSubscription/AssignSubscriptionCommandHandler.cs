using System;
using System.Collections.Generic;
using System.Text;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.v1.Subscriptions;
using Modules.Billing.Data;
using Modules.Billing.Domain;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Subscriptions.AssignSubscription;

public sealed class AssignSubscriptionCommandHandler : ICommandHandler<AssignSubscriptionCommand, Guid>
{
    private readonly BillingDbContext _dbContext;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public AssignSubscriptionCommandHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    {
        _dbContext = dbContext;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }
    public async ValueTask<Guid> Handle(AssignSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        var callerTenantId = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedAccessException("Caller tenant ID is not available");

        var tenantId = callerTenantId == MultitenancyConstants.Root.Id ? command.TenantId : callerTenantId;

        var key = command.PlanKey.ToLowerInvariant();

        var plan = await _dbContext.Plans.FirstOrDefaultAsync(p => p.Key == key && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found or not active");

        var now = DateTime.UtcNow;

        var current = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == Contracts.SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        current?.Cancel(now);

        var subscription = Subscription.Create(tenantId, plan.Id, now);

        await _dbContext.Subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
       
        return subscription.Id;
    }
}
