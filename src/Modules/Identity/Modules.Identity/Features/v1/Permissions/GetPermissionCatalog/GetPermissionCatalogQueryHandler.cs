using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Permissions.GetPermissionCatalog;

using Shared.Identity;
using Shared.Multitenancy;

namespace Modules.Identity.Features.v1.Permissions.GetPermissionCatalog;

public class GetPermissionCatalogQueryHandler(IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : IQueryHandler<GetPermissionCatalogQuery, IReadOnlyList<PermissionCatalogEntryDto>>
{

    public ValueTask<IReadOnlyList<PermissionCatalogEntryDto>> Handle(GetPermissionCatalogQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId = tenantAccessor.MultiTenantContext.TenantInfo?.Id;
        bool isRoot = string.Equals(tenantId, MultitenancyConstants.Root.Id, StringComparison.Ordinal);
        
        // Matches the same root-vs-admin rule used by RolePermissionSyncer so the catalog the
        // SPA edits agrees with the set the syncer would push into a tenant's role claims.
        var source = isRoot 
            ? PermissionConstants.Admin.Concat(PermissionConstants.Root).DistinctBy(x => x.Name)
                : PermissionConstants.Admin;
        
        IReadOnlyList<PermissionCatalogEntryDto> result = 
        [
            .. source.Select(p => new PermissionCatalogEntryDto(
                p.Name,
                p.Description,
                p.Resource,
                p.Action,
                p.IsBasic,
                p.IsRoot))
        ] ;

        return ValueTask.FromResult(result);
    }
}