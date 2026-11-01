using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Billing.Contracts.Dtos;

public sealed record SubscriptionDto(
    Guid Id,
    string TenantId,
    Guid PlanId,
    string PlanKey,
    DateTime StartUtc,
    DateTime? EndUtc,
    SubscriptionStatus Status);
