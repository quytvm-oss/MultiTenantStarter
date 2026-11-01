using System;
using System.Collections.Generic;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Billing.Contracts.Dtos;
using Modules.Billing.Contracts.v1.Invoices;
using Modules.Billing.Data;
using Modules.Billing.Mappings;

using Persistence.Pagination;

using Shared.Multitenancy;
using Shared.Persistence;

namespace Modules.Billing.Features.v1.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler : IQueryHandler<GetInvoicesQuery, PagedResponse<InvoiceDto>>
{
    private readonly BillingDbContext _dbContext;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;

    public GetInvoicesQueryHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> accessor)
    {
        _dbContext = dbContext;
        _accessor = accessor;
    }

    public async ValueTask<PagedResponse<InvoiceDto>> Handle(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var callerTenantId = _accessor.MultiTenantContext?.TenantInfo?.Id 
            ?? throw new UnauthorizedException("Tenant is require.");

        var isRoot = callerTenantId == MultitenancyConstants.Root.Id;
        var tenantFilter = isRoot ? query.TenantId : callerTenantId;

        var q = _dbContext.Invoices.AsNoTracking().Include(x => x.LineItems).AsQueryable();

        if (!string.IsNullOrWhiteSpace(tenantFilter))
        {
            q = q.Where(x => x.TenantId == tenantFilter);
        }

        if (query.Status is not null)
        {
            q = q.Where(x => x.Status == query.Status);
        }

        if (query.PeriodYear is not null)
        {
            q = q.Where(i => i.PeriodYear == query.PeriodYear);
        }

        if (query.PeriodMonth is not null)
        {
            q = q.Where(i => i.PeriodMonth == query.PeriodMonth);
        }

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var invoices = await q.OrderByDescending(x => x.CreatedAtUtc)
            .Skip(EfCoreQueryableExtensions.GetOffset(query.PageNumber, query.PageSize))
            .Take(query.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResponse<InvoiceDto>
        {
            Items = invoices.Select(i => i.ToDto()).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
        };
    }
}
