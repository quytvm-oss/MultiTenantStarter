using Core.Domain;

using Mediator;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Persistence.Inteceptors;

public class DomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventsInterceptor> _logger;

    public DomainEventsInterceptor(IPublisher publisher, ILogger<DomainEventsInterceptor> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        var domainEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .SelectMany(x =>
            {
                var pending = x.Entity.DomainEvents.ToArray();
                x.Entity.ClearDomainEvents();
                return pending;
            }).ToArray();

        if (domainEvents.Length == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Publishing {Count} domain events...", domainEvents.Length);
        }

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Handler failures must not fail the already-committed save (events collected post-
                // SaveChanges). Handlers needing guaranteed delivery should use the outbox pattern.
                _logger.LogError(e, "Failed to publish domain event {EventType}", domainEvent.GetType().Name);
            }
        }
        
        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }
}