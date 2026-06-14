using System.Diagnostics;

using Core.Context;

using Microsoft.AspNetCore.Http;

using Shared.Identity.Claims;

namespace Web.Auth;

public class CurrentUserMiddleware : IMiddleware
{
    private readonly ICurrentUserInitializer _currentUserInitializer;

    public CurrentUserMiddleware(ICurrentUserInitializer currentUserInitializer)
    {
        _currentUserInitializer = currentUserInitializer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        
        _currentUserInitializer.SetCurrentUser(context.User);
        
        var activity = Activity.Current;

        if (activity is not null && context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.GetUserId();
            var tenant = context.User.GetTenant();
            var correlationId = context.Request.HttpContext.TraceIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                activity.SetTag("user_id", userId);
            }
            
            if (!string.IsNullOrEmpty(tenant))
            {
                activity.SetTag("tenant_id", tenant);
            }
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag("correlation_id", correlationId);
            }
        }
        
        await next(context);
    }
}