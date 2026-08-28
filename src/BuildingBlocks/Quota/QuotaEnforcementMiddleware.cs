namespace Quota;

/// <summary>
/// Per-request quota enforcement for <see cref="QuotaResource.ApiCalls"/>. Runs after auth and the
/// rate limiter so only authenticated, counted calls charge the meter. Skips health probes, the
/// root tenant, and unresolved tenants. Rejects with HTTP 429 + RFC 9457 ProblemDetails and a
/// <c>Retry-After</c> header; flags the request via <see cref="HttpContextItemKeys.QuotaRejected"/>
/// so the audit middleware can tag the activity event with <c>AuditTag.OutOfQuota</c> without this
/// middleware taking a dependency on the auditing module.
/// </summary>
public class QuotaEnforcementMiddleware
{
    
}