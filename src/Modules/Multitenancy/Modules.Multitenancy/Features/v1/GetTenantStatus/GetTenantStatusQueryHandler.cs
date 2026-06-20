using Mediator;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.GetTenantStatus;

namespace Modules.Multitenancy.Features.v1.GetTenantStatus;

public class GetTenantStatusQueryHandler : IQueryHandler<GetTenantStatusQuery,TenantStatusDto>
{
    private readonly ITenantService _tenantService;

    public GetTenantStatusQueryHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async ValueTask<TenantStatusDto> Handle(GetTenantStatusQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _tenantService.GetStatusAsync(query.TenantId, cancellationToken).ConfigureAwait(false);
    }
}