using System;
using System.Collections.Generic;
using System.Text;

using Finbuckle.MultiTenant.Abstractions;

using Mediator;

using Modules.Billing.Contracts.Dtos;
using Modules.Billing.Contracts.v1.Usage;
using Modules.Billing.Services;

using Shared.Multitenancy;

namespace Modules.Billing.Features.v1.Usage.CaptureUsageSnapshots;

public sealed class CaptureUsageSnapshotsCommandHandler : ICommandHandler<CaptureUsageSnapshotsCommand, IEnumerable<UsageSnapshotDto>>
{
    private readonly IUsageReporter _reporter;

    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    public CaptureUsageSnapshotsCommandHandler(IUsageReporter reporter, IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    {
        _reporter = reporter;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }
    

    public async ValueTask<IEnumerable<UsageSnapshotDto>> Handle(CaptureUsageSnapshotsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        var callerTenantId = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedAccessException("Caller tenant ID is not available");

        var tenantId = callerTenantId == MultitenancyConstants.Root.Id ? command.TenantId : callerTenantId;

        var snapshots = await _reporter.CaptureForPeriodAsync(tenantId, command.PeriodYear, command.PeriodMonth, cancellationToken)
            .ConfigureAwait(false);

        return snapshots.Select(x => new UsageSnapshotDto(
            x.Id,
            x.TenantId,
            x.PeriodYear,
            x.PeriodMonth,
            x.Resource,
            x.UsedUnits,
            x.LimitUnits,
            x.Overage,
            x.CapturedAtUtc)).ToList();
    }
}
