using System.Reflection;

using Mediator;

using Microsoft.Extensions.DependencyInjection;

using Web.Mediator.Behaviors;

namespace Web.Mediator;

public static class Extensions
{
    public static IServiceCollection EnableMediator(this IServiceCollection services, params Assembly[] featureAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    
        //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        return services;
    }
}