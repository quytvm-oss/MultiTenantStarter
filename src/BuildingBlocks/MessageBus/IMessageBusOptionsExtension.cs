using Microsoft.Extensions.DependencyInjection;

namespace MessageBus;

public interface IMessageBusOptionsExtension
{
    void AddExtendServices(IServiceCollection services);
}