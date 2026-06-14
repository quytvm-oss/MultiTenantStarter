using MessageBus.Contracts;

namespace MessageBus;

public interface IConsumerRegistration
{
    //void RegisterServices(IServiceCollection services);
    
    void Register(ISubscribeBusBuilder builder);
}