using System;
using System.Collections.Generic;
using System.Text;

using Mediator;

using Modules.Billing.Contracts.Dtos;

namespace Modules.Billing.Contracts.v1.Usage;

/// <summary>
/// Ops command that captures one usage snapshot per <c>QuotaResource</c> for a tenant + period.
/// Wraps <c>IUsageReporter.CaptureForPeriodAsync</c>. Idempotent: re-running for the same
/// (tenant, period) returns the existing snapshots without creating duplicates.
/// </summary>
public sealed record CaptureUsageSnapshotsCommand(
    string TenantId,
    int PeriodYear,
    int PeriodMonth) : ICommand<IEnumerable<UsageSnapshotDto>>;
