using Mediator;

namespace Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;

public sealed record DeleteWebhookSubscriptionCommand(Guid Id) : ICommand;