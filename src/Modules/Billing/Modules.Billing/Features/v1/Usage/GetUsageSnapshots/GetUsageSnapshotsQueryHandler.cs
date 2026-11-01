using System;
using System.Collections.Generic;
using System.Text;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.Dtos;
using Modules.Billing.Contracts.v1.Usage;
using Modules.Billing.Data;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Usage.GetUsageSnapshots;

public class GetUsageSnapshotsQueryHandler : IQueryHandler<GetUsageSnapshotsQuery, IEnumerable<UsageSnapshotDto>>
{
    private readonly BillingDbContext _dbContext;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public GetUsageSnapshotsQueryHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    {
        _dbContext = dbContext;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public async ValueTask<IEnumerable<UsageSnapshotDto>> Handle(GetUsageSnapshotsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var callerTenantId = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedAccessException("Caller tenant ID is not available");

        var tenantId = callerTenantId == MultitenancyConstants.Root.Id ? request.TenantId : callerTenantId;

        var query = _dbContext.UsageSnapshots.AsNoTracking();

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(x => x.TenantId == tenantId);
        }
        if (request.PeriodYear is not null)
        {
            query = query.Where(x => x.PeriodYear == request.PeriodYear);
        }
        if (request.PeriodMonth is not null)
        {
            query = query.Where(x => x.PeriodMonth == request.PeriodMonth);
        }

        var snap = await query.OrderByDescending(s => s.PeriodYear)
            .ThenByDescending(s => s.PeriodMonth)
            .ThenBy(s => s.TenantId).ThenBy(s => s.Resource)
            .Select( s => new UsageSnapshotDto(
                s.Id, 
                s.TenantId, 
                s.PeriodYear, 
                s.PeriodMonth, 
                s.Resource, 
                s.UsedUnits, 
                s.LimitUnits,
                s.Overage, 
                s.CapturedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return snap;
    }
}
