using Shared.Identity;

namespace Modules.Billing.Contracts.Authorization;

public static class BillingPermissions
{
    public const string Resource = "Billing";
    public const string View = $"Permissions.{Resource}.View";
    public const string Manage = $"Permissions.{Resource}.Manage";

    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View Billing",   ActionConstants.View, Resource, IsBasic: true),
        new("Manage Billing", "Manage",             Resource),
    ];
}
