using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.UpdateTenantTheme;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Features.v1.UpdateTenantTheme;

public class UpdateTenantThemeCommandHandler(
    ITenantThemeService tenantThemeService,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<UpdateTenantThemeCommand>
{

    public async ValueTask<Unit> Handle(UpdateTenantThemeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new InvalidOperationException("No tenant context available");

        await tenantThemeService.UpdateThemeAsync(tenantId, command.Theme, cancellationToken);
        
        return Unit.Value;
    }
}