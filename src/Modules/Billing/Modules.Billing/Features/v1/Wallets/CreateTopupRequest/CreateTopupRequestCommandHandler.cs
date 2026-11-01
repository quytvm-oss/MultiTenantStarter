using System;
using System.Collections.Generic;
using System.Text;

using Core.Context;
using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Modules.Billing.Contracts.v1.Wallets;
using Modules.Billing.Data;
using Modules.Billing.Domain;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Wallets.CreateTopupRequest;

public sealed class CreateTopupRequestCommandHandler : ICommandHandler<CreateTopupRequestCommand, Guid>
{
    private readonly BillingDbContext _dbContext;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;

    private readonly ICurrentUser _currentUser;

    public CreateTopupRequestCommandHandler(BillingDbContext dbContext, IMultiTenantContextAccessor<AppTenantInfo> accessor, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _accessor = accessor;
        _currentUser = currentUser;
    }

    public async ValueTask<Guid> Handle(CreateTopupRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // BillingDbContext is not tenant-filtered; resolve caller's own tenant and scope strictly to it.
        var tenantId = _accessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant is required.");

        var requestBy = _currentUser.IsAuthenticated() ? _currentUser.GetUserId().ToString() : null;
        var request = TopupRequest.Create(tenantId, command.Amount, "USD", command.Note, requestBy);
        await _dbContext.AddAsync(request);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return request.Id;       
    }
}
