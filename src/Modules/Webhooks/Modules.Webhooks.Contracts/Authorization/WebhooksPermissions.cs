using Shared.Identity;

namespace Modules.Webhooks.Contracts.Authorization;

public class WebhooksPermissions
{
    public static class Subscriptions
    {
        public const string Resource = "Webhooks";
        public const string View = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Delete = $"Permissions.{Resource}.Delete";
        public const string Test = $"Permissions.{Resource}.Test";
    }

    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View Webhooks",   ActionConstants.View,   Subscriptions.Resource, IsBasic: true),
        new("Create Webhooks", ActionConstants.Create, Subscriptions.Resource),
        new("Delete Webhooks", ActionConstants.Delete, Subscriptions.Resource),
        new("Test Webhooks",   "Test",                 Subscriptions.Resource),
    ];
}
