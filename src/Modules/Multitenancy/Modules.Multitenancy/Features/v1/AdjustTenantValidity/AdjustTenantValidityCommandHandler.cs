using Mediator;

using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.AdjustTenantValidity;

namespace Modules.Multitenancy.Features.v1.AdjustTenantValidity;

public class AdjustTenantValidityCommandHandler : IQueryHandler<AdjustTenantValidityCommand, AdjustTenantValidityCommandResponse>
{
    private readonly ITenantService _tenantService;

    public AdjustTenantValidityCommandHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async ValueTask<AdjustTenantValidityCommandResponse> Handle(AdjustTenantValidityCommand query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var validUpto = await _tenantService
            .AdjustValidityAsync(query.TenantId, query.ValidUpto, cancellationToken)
            .ConfigureAwait(false);
        
        return new AdjustTenantValidityCommandResponse(query.TenantId, validUpto);
    }
}