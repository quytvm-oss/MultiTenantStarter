using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.ResetTenantTheme;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Features.v1.ResetTenantTheme;

public class ResetTenantThemeCommandHandler(
    ITenantThemeService tenantThemeService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContext)
    : ICommandHandler<ResetTenantThemeCommand>
{

    public async ValueTask<Unit> Handle(ResetTenantThemeCommand command, CancellationToken cancellationToken)
    {
        var tenantId = multiTenantContext.MultiTenantContext.TenantInfo?.Id
            ?? throw new MultiTenantException("No tenant context available");

        await tenantThemeService.ResetThemeAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}