using System.Reflection;
using MessageBus.Contracts;

namespace MessageBus;

public static class SubscribeBusBuilderExtensions
{
    public static ISubscribeBusBuilder ApplyRegistrationsFromAssembly(
        this ISubscribeBusBuilder builder,
        Assembly assembly)
    {
        var registrationTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IConsumerRegistration).IsAssignableFrom(t));

        foreach (var type in registrationTypes)
        {
            var registration = (IConsumerRegistration)Activator.CreateInstance(type)!;
            registration.Register(builder);
        }

        return builder;
    }
    
    public static ISubscribeBusBuilder ApplyRegistrations(this ISubscribeBusBuilder builder)
        => builder.ApplyRegistrationsFromAssembly(Assembly.GetCallingAssembly());
}