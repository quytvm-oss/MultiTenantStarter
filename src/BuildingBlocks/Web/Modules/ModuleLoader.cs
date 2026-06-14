using System.Reflection;

using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Web.Modules;

public static class ModuleLoader
{
    private static readonly List<IModule> _modules = new();
    private static readonly object _lock = new();
    private static bool _modulesLoaded;

    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);

        lock (_lock)
        {
            if (_modulesLoaded) return builder;

            builder.Services.AddValidatorsFromAssemblies(assemblies);
            
            var source = assemblies is { Length:>0 } ?
                assemblies :
                AppDomain.CurrentDomain.GetAssemblies();
            
            var moduleRegistrations = source.SelectMany(a => a.GetCustomAttributes<ModuleAttribute>())
                .Where(r => typeof(IModule).IsAssignableFrom(r.ModuleType))
                .DistinctBy(x => x.ModuleType)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.ModuleType.Name)
                .Select(r => r.ModuleType);

            foreach (var moduleType in moduleRegistrations)
            {
                if (Activator.CreateInstance(moduleType) is not IModule module)
                {
                    throw new InvalidOperationException($"Unable to create module {moduleType.Name}.");
                }
                
                module.ConfigureServices(builder);
                
                _modules.Add(module);
                
            }
            
            _modulesLoaded = true;
        }
        
        return builder;
    }

    public static IApplicationBuilder UseModuleMiddlewares(this IApplicationBuilder app)
    {
        foreach (var module in _modules)
        {
            module.ConfigureMiddleware(app);
        }
        return app;
    }

    public static IEndpointRouteBuilder UseModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in _modules)
        {
            module.MapEndpoints(endpoints);
        }
        return endpoints;
    }
}