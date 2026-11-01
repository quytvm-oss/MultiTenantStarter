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

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Invoices.GetInvoiceById;

public sealed class GetInvoiceByIdQueryHandler : IQueryHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly BillingDbContext _dbContext;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _contextAccessor;
    public GetInvoiceByIdQueryHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> contextAccessor)
    {
        _dbContext = dbContext;
        _contextAccessor = contextAccessor;
    }

    public async ValueTask<InvoiceDto> Handle(GetInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var callerTenantId = _contextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");
        var isRoot = callerTenantId == MultitenancyConstants.Root.Id;

        var invoice = await _dbContext.Invoices.AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(
                i => i.Id == query.InvoiceId && (isRoot || i.TenantId == callerTenantId),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {query.InvoiceId} not found.");

        return invoice.ToDto();
    }
}
