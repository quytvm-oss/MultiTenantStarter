using Mediator;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.GetTenantTheme;

namespace Modules.Multitenancy.Features.v1.GetTenantTheme;

public sealed class GetTenantThemeQueryHandler : IQueryHandler<GetTenantThemeQuery, TenantThemeDto>
{
    private readonly ITenantThemeService _tenantThemeService;

    public GetTenantThemeQueryHandler(ITenantThemeService tenantThemeService)
    {
        _tenantThemeService = tenantThemeService;
    }

    public async ValueTask<TenantThemeDto> Handle(GetTenantThemeQuery query, CancellationToken cancellationToken)
    {
        return await _tenantThemeService.GetCurrentTenantThemeAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}