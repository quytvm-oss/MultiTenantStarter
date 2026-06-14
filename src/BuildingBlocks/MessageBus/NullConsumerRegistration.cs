using MessageBus.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus;

public class NullConsumerRegistration : IConsumerRegistration
{
    public void RegisterServices(IServiceCollection services)
    {
        //return services;
    }

    public void Register(ISubscribeBusBuilder builder)
    {
        
    }
}