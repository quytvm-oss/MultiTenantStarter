using Rebus.Bus;

namespace Web.MessageBus;

public interface IRebusSubscription
{
    Task SubscribeAsync(IBus bus, CancellationToken cancellationToken);
}
