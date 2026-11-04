using System;
using System.Collections.Generic;
using System.Text;

using Rebus.ServiceProvider;

namespace Messaging.Rebus;

public interface IRebusSubscription
{
    Task SubscribeAsync(IBusRegistry bus, CancellationToken cancellationToken);
}
