using System;
using System.Collections.Generic;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.Dtos;
using Modules.Billing.Contracts.v1.Wallets;
using Modules.Billing.Data;
using Modules.Billing.Mappings;

using Persistence.Pagination;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Billing.Features.v1.Wallets.GetTopupRequests;

public sealed class GetTopupRequestsQueryHandler : IQueryHandler<GetTopupRequestsQuery, PagedResponse<TopupRequestDto>>
{
    private readonly BillingDbContext _dbContext;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;

    public GetTopupRequestsQueryHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> accessor)
    {
        _dbContext = dbContext;
        _accessor = accessor;
    }

    public async ValueTask<PagedResponse<TopupRequestDto>> Handle(GetTopupRequestsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // BillingDbContext is not tenant-filtered: only root gets the cross-tenant view (optionally
        // narrowed via query.TenantId); every other caller is forced to its own tenant.
        var callerTenantId = _accessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");
        var tenantFilter = callerTenantId == MultitenancyConstants.Root.Id ? query.TenantId : callerTenantId;

        var q = _dbContext.TopupRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tenantFilter))
        {
            q = q.Where(x => x.TenantId == tenantFilter);
        }

        if (query.Status is not null)
        {
            q = q.Where(x => x.Status == query.Status);
        }

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q.OrderByDescending(t => t.CreatedAtUtc)
            .Skip(EfCoreQueryableExtensions.GetOffset(query.PageNumber, query.PageSize))
            .Take(query.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResponse<TopupRequestDto>
        {
            Items = items.Select(t => t.ToDo()).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
        };
    }
}
