using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

using Shared.Multitenancy;

namespace Web.FeatureFlags;

/// <summary>
/// A feature filter that enables/disables features based on the current tenant.
/// Configure in appsettings.json with allowed tenant IDs.
/// </summary>
[FilterAlias("Tenant")]
public class TenantFeatureFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMultiTenantContextAccessor<AppTenantInfo>? tenantContextAccessor;

    public TenantFeatureFilter(IHttpContextAccessor httpContextAccessor, IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        tenantContextAccessor = multiTenantContextAccessor;
    }

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tenantId = tenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            tenantId = _httpContextAccessor.HttpContext?.Request.Headers[MultitenancyConstants.Identifier].ToString();
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Task.FromResult(false);
        }
        
        var allowedTenantIds = context.Parameters.GetSection("AllowedTenants").Get<string[]>() ?? [];
        var result = allowedTenantIds.Contains(tenantId, StringComparer.OrdinalIgnoreCase);
        
        return Task.FromResult(result);
    }
}