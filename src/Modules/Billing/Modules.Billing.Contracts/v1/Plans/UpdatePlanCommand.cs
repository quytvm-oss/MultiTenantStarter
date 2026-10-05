using Mediator;

using Shared.Quota;

namespace Modules.Billing.Contracts.v1.Plans;

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Name,
    decimal MonthlyBasePrice,
    IReadOnlyDictionary<QuotaResource, decimal>? OverageRates = null,
    PlanInterval Interval = PlanInterval.Monthly,
    decimal? AnnualPrice = null) : ICommand<Guid>;
