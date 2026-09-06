using Rebus.Bus;
using Rebus.ServiceProvider;

namespace Web.MessageBus;

public interface IRebusSubscription
{
    Task SubscribeAsync(IBusRegistry bus, CancellationToken cancellationToken);
}
