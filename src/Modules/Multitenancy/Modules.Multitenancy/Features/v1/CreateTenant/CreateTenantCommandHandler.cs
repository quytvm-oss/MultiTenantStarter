using Mediator;

using Messaging.Rebus;

using Microsoft.Extensions.Options;

using Modules.Billing.Contracts.Events;
using Modules.Billing.Contracts.v1.Plans;
using Modules.Multitenancy.Contracts.Events;
using Modules.Multitenancy.Contracts.v1;
using Modules.Multitenancy.Contracts.v1.CreateTenant;
using Modules.Multitenancy.Provisioning;

using Rebus.Bus;

using Shared.Multitenancy;

namespace Modules.Multitenancy.Features.v1.CreateTenant;

public class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, CreateTenantCommandResponse>
{
    private readonly ITenantService _tenantService;
    private readonly ITenantProvisioningStarter provisioningService;
    private readonly ITenantInitialPasswordBuffer passwordBuffer;
    private readonly IOptions<TenantBillingOptions> _options;
    private readonly IMediator mediator;
    private readonly IModuleBus<MultitenancyModule> _bus;

    private readonly TimeProvider _timeProvider;

    public CreateTenantCommandHandler(ITenantService tenantService,
        ITenantProvisioningStarter provisioningService,
        ITenantInitialPasswordBuffer passwordBuffer,
        IMediator mediator,
        IModuleBus<MultitenancyModule> bus,
        TimeProvider timeProvider,
        IOptions<TenantBillingOptions> options)
    {
        _tenantService = tenantService;
        this.provisioningService = provisioningService;
        this.passwordBuffer = passwordBuffer;
        this.mediator = mediator;
        this._bus = bus;
        this._timeProvider = timeProvider;
        _options = options;
    }

    public async ValueTask<CreateTenantCommandResponse> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Resolve the plan (falls back to trial) and read its term to set the tenant validity
        // window. A bad plan key throws NotFound (400) before any tenant is created.
        var planKey = string.IsNullOrWhiteSpace(command.PlanKey)
            ? _options.Value.DefaultPlanKey
            : command.PlanKey!;
         var term = await mediator.Send(new GetPlanTermQuery(planKey), cancellationToken).ConfigureAwait(false);

        var periodStart = _timeProvider.GetUtcNow().UtcDateTime;
        var periodEnd = periodStart.AddMonths(3);

        var tenantId = await _tenantService.CreateAsync(
            command.Id,
            command.Name,
            command.ConnectionString,
            command.AdminEmail,
            command.Issuer,
            term.Key,
            periodEnd,
            cancellationToken).ConfigureAwait(false);

        // Buffer the admin password for IdentityDbInitializer's background seed step,
        // storing it before StartAsync so the seed never runs ahead of the buffer.
        passwordBuffer.Store(tenantId, command.AdminPassword);

        var provisioning = await provisioningService.StartAsync(tenantId, cancellationToken).ConfigureAwait(false);

        // Drive the billing side-effects (subscription + term invoice) via an integration event so
        // Multitenancy stays decoupled from the Billing runtime.
        await _bus.Publish(new TenantSubscribedIntegrationEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: periodStart,
            TenantId: tenantId,
            CorrelationId: provisioning.CorrelationId,
            Source: "Multitenancy",
            PlanId: term.PlanId,
            PlanKey: term.Key,
            PeriodStartUtc: periodStart,
            PeriodEndUtc: periodEnd)).ConfigureAwait(false);

        //await _bus.Publish(new InvoiceIssuedIntegrationEvent(
        //Id: Guid.CreateVersion7(),
        //OccurredOnUtc: _timeProvider.GetUtcNow().UtcDateTime,
        //TenantId: tenantId,
        //CorrelationId: Guid.NewGuid().ToString(),
        //Source: "Billing",
        //InvoiceId: Guid.CreateVersion7(),
        //InvoiceNumber: "invoice.InvoiceNumber",
        //Amount:24435,
        //Currency: "invoice.Currenc",
        //DueAtUtc: periodStart,
        //PeriodYear: 2016,
        //PeriodMonth: 12));

        return new CreateTenantCommandResponse(
            tenantId,
            provisioning.CorrelationId,
            provisioning.Status.ToString());
    }
}