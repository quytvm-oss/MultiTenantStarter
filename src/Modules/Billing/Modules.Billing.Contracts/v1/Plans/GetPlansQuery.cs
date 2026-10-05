using Mediator;

using Modules.Billing.Contracts.Dtos;

namespace Modules.Billing.Contracts.v1.Plans;

public sealed record GetPlansQuery(bool IncludeInactive = false) : IQuery<IReadOnlyList<BillingPlanDto>>;