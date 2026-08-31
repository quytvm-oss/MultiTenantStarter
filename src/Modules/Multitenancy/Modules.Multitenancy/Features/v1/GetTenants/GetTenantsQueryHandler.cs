using System.Linq.Expressions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Modules.Multitenancy.Contracts.Dtos;
using Modules.Multitenancy.Contracts.v1.GetTenants;
using Modules.Multitenancy.Data;

using Persistence.Pagination;

using Shared.Persistence;

namespace Modules.Multitenancy.Features.v1.GetTenants;

public class GetTenantsQueryHandler : IQueryHandler<GetTenantsQuery, PagedResponse<TenantDto>>
{
    private readonly TenantDbContext _tenantDbContext;

    public GetTenantsQueryHandler(TenantDbContext tenantDbContext)
    {
        _tenantDbContext = tenantDbContext;
    }

    public async ValueTask<PagedResponse<TenantDto>> Handle(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<TenantDto> tenants = _tenantDbContext.TenantInfo
            .AsNoTracking()
            .Select(x => new TenantDto()
            {
                Id = x.Id!,
                Name = x.Name,
                ConnectionString = x.ConnectionString,
                AdminEmail = x.AdminEmail,
                IsActive = x.IsActive,
                ValidToUp = x.ValidUpTo,
                Issuer = x.Issuer
            });

        tenants = ApplySorting(tenants, query.Sort);

        return await tenants.ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }

    private static IQueryable<TenantDto> ApplySorting(
        IQueryable<TenantDto> query,
        string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return ApplyDefaultSorting(query);
        }

        IOrderedQueryable<TenantDto>? ordered = null;

        foreach (var raw in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            bool descending = raw.StartsWith("-");
            string key = raw.TrimStart('-', '+').ToLowerInvariant();

            ordered = key switch
            {
                "id" => ApplySort(query, ordered, x => x.Id, descending),
                "name" => ApplySort(query, ordered, x => x.Name, descending),
                "connectionstring" => ApplySort(query, ordered, x => x.ConnectionString, descending),
                "adminemail" => ApplySort(query, ordered, x => x.AdminEmail, descending),
                "isactive" => ApplySort(query, ordered, x => x.IsActive, descending),
                "validupto" => ApplySort(query, ordered, x => x.ValidToUp, descending),
                "issuer" => ApplySort(query, ordered, x => x.Issuer, descending),
                _ => ordered
            };
        }

        return ordered ?? ApplyDefaultSorting(query);
    }

    private static IOrderedQueryable<TenantDto> ApplyDefaultSorting(IQueryable<TenantDto> query)
    {
        return query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id);
    }

    private static IOrderedQueryable<TenantDto> ApplySort<TKey>(
        IQueryable<TenantDto> source,
        IOrderedQueryable<TenantDto>? ordered,
        Expression<Func<TenantDto, TKey>> selector,
        bool descending)
    {
        if (ordered is null)
        {
            return descending
                ? source.OrderByDescending(selector)
                : source.OrderBy(selector);
        }

        return descending
            ? ordered.ThenByDescending(selector)
            : ordered.ThenBy(selector);
    }
}