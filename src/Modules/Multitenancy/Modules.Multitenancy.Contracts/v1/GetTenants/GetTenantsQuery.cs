using Mediator;

using Modules.Multitenancy.Contracts.Dtos;

using Shared.Persistence;

namespace Modules.Multitenancy.Contracts.v1.GetTenants;

public sealed class GetTenantsQuery : IPagedQuery, IQuery<PagedResponse<TenantDto>>
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    
    //public bool Descending { get; init; } = true;
    
    public string? Sort { get; set; }
}