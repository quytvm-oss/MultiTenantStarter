using System;
using System.Collections.Generic;
using System.Text;

using Core.Exceptions;
using Core.Messaging;

using Finbuckle.MultiTenant.Abstractions;

using MassTransit;

using Microsoft.Extensions.Logging;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Filters;

public sealed class TenantConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class, IIntegrationEvent
{
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly IMultiTenantContextSetter _setter;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor;
    private readonly ILogger<TenantConsumeFilter<T>> _logger;

    public TenantConsumeFilter(
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IMultiTenantContextSetter setter,
        IMultiTenantContextAccessor<AppTenantInfo> accessor,
        ILogger<TenantConsumeFilter<T>> logger)
    {
        _tenantStore = tenantStore;
        _setter = setter;
        _accessor = accessor;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var tenantId = context.Message?.TenantId;

        // Global event
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            await next.Send(context);
            return;
        }

        var tenant = await _tenantStore.GetAsync(tenantId);

        if (tenant is null)
        {
            throw new NotFoundException($"Tenant '{tenantId}' not found.");
        }

        var previous = _accessor.MultiTenantContext;

        try
        {
            _setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            _logger.LogDebug("Tenant context initialized. TenantId={TenantId}",tenantId);

            await next.Send(context);
        }
        finally
        {
            _setter.MultiTenantContext = previous;
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenantConsumeFilter");
    }
}
