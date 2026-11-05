using System;
using System.Collections.Generic;
using System.Text;

using Amazon.Util.Internal;

using Mediator;

using Messaging.Rebus;

using Microsoft.Extensions.Options;

using Modules.Billing.Contracts.v1.Plans;
using Modules.Multitenancy.Contracts.Events;
using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.RenewTenant;

namespace Modules.Multitenancy.Features.v1.RenewTenant;

public sealed class RenewTenantCommandHandler : ICommandHandler<RenewTenantCommand, RenewTenantCommandResponse>
{
    private readonly ITenantService _tenantService;
    private readonly IMediator _mediator;
    private readonly IOptions<TenantBillingOptions> _options;
    private readonly IModuleBus<MultitenancyModule> _bus;
    private readonly TimeProvider _provider;

    public RenewTenantCommandHandler(ITenantService tenantService, IMediator mediator, IOptions<TenantBillingOptions> options, IModuleBus<MultitenancyModule> bus, TimeProvider provider)
    {
        _tenantService = tenantService;
        _mediator = mediator;
        _options = options;
        _bus = bus;
        _provider = provider;
    }

    public async ValueTask<RenewTenantCommandResponse> Handle(RenewTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Target plan: explicit key, else the tenant's current plan, else the configured default.
        var status = await _tenantService.GetStatusAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        var targetKey = command.PlanKey;

        if (string.IsNullOrWhiteSpace(targetKey))
        {
            targetKey = string.IsNullOrWhiteSpace(status.Plan) ? _options.Value.DefaultPlanKey : status.Plan;
        }

        var term = await _mediator.Send(new GetPlanTermQuery(targetKey), cancellationToken).ConfigureAwait(false);
        var(periodStart, validUpto, planChanged) = await _tenantService.RenewAsync(command.TenantId, term.Key,term.TermMonths, cancellationToken)
            .ConfigureAwait(false);

        await _bus.Publish(new TenantRenewedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: _provider.GetUtcNow().UtcDateTime,
            TenantId: command.TenantId,
            CorrelationId: Guid.NewGuid().ToString(),
            Source: "Multitenancy",
            PlanId: term.PlanId,
            PlanKey: term.Key,
            PeriodStartUtc: periodStart,
            PeriodEndUtc: validUpto,
            PlanChanged: planChanged));

        return new RenewTenantCommandResponse(command.TenantId, validUpto, term.Key, planChanged);
    }
}
