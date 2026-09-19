using Core.Domain;
using Core.Domain.ValueObjects;

using Modules.Billing.Contracts;

namespace Modules.Billing.Domain;

/// <summary>
/// Priced side of a tenant plan. The plan key matches the key used by quota configuration so a
/// plan named "pro" in QuotaOptions.Plans corresponds to the BillingPlan with Key "pro". Limits
/// come from QuotaOptions; prices and overage rates come from here.
///
/// <see cref="IGlobalEntity"/>: plans are platform-wide catalogue rows, NOT per-tenant.
/// Every tenant subscribes to one of these shared plans.
/// </summary>
public class BillingPlan : BaseEntity<Guid>, IGlobalEntity
{
    public string Key { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public Money MonthlyBasePrice { get; private set; } = default!;

    public string Currency => MonthlyBasePrice.Currency;

    public PlanInterval Interval { get; private set; } = PlanInterval.Monthly;

    /// <summary>
    /// Flat price charged per yearly term. Only meaningful when <see cref="Interval"/> is
    /// <see cref="PlanInterval.Yearly"/>; <c>null</c> falls back to twelve times the monthly base
    /// price so a yearly plan can be configured without restating the discount.
    /// </summary>
    public Money? AnnualPrice { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc { get; private set; }


}
