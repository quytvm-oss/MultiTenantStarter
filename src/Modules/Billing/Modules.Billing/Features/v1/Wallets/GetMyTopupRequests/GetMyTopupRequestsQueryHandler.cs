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

namespace Modules.Billing.Features.v1.Wallets.GetMyTopupRequests;

public sealed class GetMyTopupRequestsQueryHandler : IQueryHandler<GetMyTopupRequestsQuery, PagedResponse<TopupRequestDto>>
{
    private readonly BillingDbContext _dbContext;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;

    public GetMyTopupRequestsQueryHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> accessor)
    {
        _dbContext = dbContext;
        _accessor = accessor;
    }

    public async ValueTask<PagedResponse<TopupRequestDto>> Handle(GetMyTopupRequestsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId = _accessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant is required.");

        var q = _dbContext.TopupRequests.AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (query.Status is not null)
        {
            q.Where(r => r.Status == query.Status);
        }

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.CreatedAtUtc)
            .Skip(EfCoreQueryableExtensions.GetOffset(query.PageNumber, query.PageSize))
            .Take(query.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);


        return new PagedResponse<TopupRequestDto>
        {
            Items = items.Select(t => t.ToDo()).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = (int) Math.Ceiling(total / (double)query.PageSize)
        };
    }
}
