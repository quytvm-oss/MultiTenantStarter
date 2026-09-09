using FluentValidation;

using Modules.Webhooks.Contracts.v1.GetWebhookSubscriptions;

namespace Modules.Webhooks.Features.v1.GetWebhookSubscriptions;

public class GetWebhookSubscriptionsQueryValidator : AbstractValidator<GetWebhookSubscriptionsQuery>
{
    public GetWebhookSubscriptionsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
