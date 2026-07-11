using System.Diagnostics;
using System.Security.Claims;

using Core.Context;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Http;

using Modules.Auditing.Contracts;

using Shared.Multitenancy;

namespace Modules.Auditing.Core;

public class HttpAuditScope(
    IHttpContextAccessor http,
    IMultiTenantContextAccessor<AppTenantInfo> tenant,
    ICurrentUser? currentUser = null)
    : IAuditScope
{
    public string? TenantId => tenant.MultiTenantContext?.TenantInfo?.Id 
                               ?? http.HttpContext?.User?.FindFirstValue(MultitenancyConstants.Identifier)
                               ?? http.HttpContext?.Request?.Headers[MultitenancyConstants.Identifier].FirstOrDefault()
                               ?? http.HttpContext?.Items["TenantId"] as string
                               ?? currentUser?.GetTenantId();
    
    public string? UserId =>
        http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? http.HttpContext?.User?.FindFirstValue("sub")
        ?? NullIfEmpty(currentUser?.GetUserId().ToString());
    
    public string? UserName => 
        http.HttpContext?.User?.Identity?.Name
       ?? http.HttpContext?.User?.FindFirstValue("name")
       ?? currentUser?.Name;
    
    public string? TraceId => Activity.Current?.TraceId.ToString();
    public string? SpanId => Activity.Current?.SpanId.ToString();
    
    public string? CorrelationId =>
        http.HttpContext?.TraceIdentifier
        ?? Activity.Current?.RootId;
    
    public string? RequestId =>
        http.HttpContext?.TraceIdentifier
        ?? Activity.Current?.Id;
    
    public string? Source =>
        http.HttpContext?.GetEndpoint()?.DisplayName
        ?? Activity.Current?.OperationName
        ?? "background";
    
    public AuditTag Tags => AuditTag.None;
    
    public IAuditScope WithTags(AuditTag tags) => this; 

    public IAuditScope WithProperties(string? tenantId = null, string? userId = null, string? userName = null, string? traceId = null,
        string? spanId = null, string? correlationId = null, string? requestId = null, string? source = null, AuditTag? tags = null) => this;
    
    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrEmpty(s) || s == Guid.Empty.ToString() ? null : s;
}