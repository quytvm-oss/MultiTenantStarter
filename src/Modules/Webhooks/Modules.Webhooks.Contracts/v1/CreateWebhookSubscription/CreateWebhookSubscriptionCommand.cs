using Mediator;

namespace Modules.Webhooks.Contracts.v1;

public sealed record CreateWebhookSubscriptionCommand(
    string Url,
    string[] Events,
    string? Secret) : ICommand<Guid>;
