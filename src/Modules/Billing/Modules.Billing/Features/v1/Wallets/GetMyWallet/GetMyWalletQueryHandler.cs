using System;
using System.Collections.Generic;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Modules.Billing.Contracts.Dtos;
using Modules.Billing.Contracts.v1.Wallets;
using Modules.Billing.Mappings;
using Modules.Billing.Services;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Wallets.GetMyWallet;

public sealed class GetMyWalletQueryHandler : IQueryHandler<GetMyWalletQuery, WalletDto>
{
    private readonly IBillingService _billingService;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenant;

    public GetMyWalletQueryHandler(IBillingService billingService, IMultiTenantContextAccessor<AppTenantInfo> multiTenant)
    {
        _billingService = billingService;
        _multiTenant = multiTenant;
    }

    public async ValueTask<WalletDto> Handle(GetMyWalletQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId = _multiTenant.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant is required.");

        var wallet = await _billingService.GetOrCreateWalletAsync(tenantId, "USD", cancellationToken).ConfigureAwait(false);

        return wallet.ToDo();
    }
}
