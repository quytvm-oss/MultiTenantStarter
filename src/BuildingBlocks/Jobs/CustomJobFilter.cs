using Core.Common;

using Finbuckle.MultiTenant.Abstractions;

using Hangfire.Client;
using Hangfire.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Shared.Identity.Claims;
using Shared.Multitenancy;

namespace Jobs;

public class CustomJobFilter : IClientFilter
{
    private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
    
    private readonly IServiceProvider _serviceProvider;

    public CustomJobFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void OnCreating(CreatingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Log.InfoFormat("Set TenantId and ParameterId to job: {0}.{1}...",
            context.Job.Method.ReflectedType?.FullName,context.Job.Method.Name);

        using var scope = _serviceProvider.CreateScope();
        
        var httContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httContextAccessor?.HttpContext;

        if (httpContext is null)
        {
            Log.WarnFormat("No HttpContext available for job {0}.{1}; skipping tenant/user parameters.",
                context.Job.Method.ReflectedType?.FullName, context.Job.Method.Name);
            return;
        }

        var mtAccessor = scope.ServiceProvider.GetService<IMultiTenantContextAccessor>();
        var tenantInfo = mtAccessor?.MultiTenantContext?.TenantInfo;
        if (tenantInfo is not null)
        {
            context.SetJobParameter(MultitenancyConstants.Identifier, tenantInfo);
        }

        var userId = httpContext.User.GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            context.SetJobParameter(QueryStringKeys.UserId, userId);
        }
    }

    public void OnCreated(CreatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var paramStr = context.Parameters.Count > 0
            ? context.Parameters
                .Select(x => $"{x.Key}={x.Value}")
                .Aggregate((s1, s2) => $"{s1};{s2}")
            : "<none>";

        Log.InfoFormat("Job created with parameters {0}", paramStr);
    }
}