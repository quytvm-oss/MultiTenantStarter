using FluentValidation;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using Modules.Files.Authorization;
using Modules.Files.Contracts;
using Modules.Files.Data;
using Modules.Files.Services;

using Persistence;

using Web.Modules;

namespace Modules.Files;

/// <summary>
/// Files module: presigned-URL file lifecycle (upload, finalize, serve, delete) shared across the
/// kit's owning features (Catalog product images, Ticket attachments, My Files, avatars, tenant
/// logos). Module order 350 places it between Auditing (300) and Webhooks (400); owning modules
/// (Catalog=600, Tickets=700) load later and register their <see cref="IFileAccessPolicy"/>
/// implementations during their own ConfigureServices.
/// </summary>
public class FilesModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddCustomDbContext<FilesDbContext>();
        builder.Services.AddScoped<IDbInitializer,FilesDbInitializer>();
        
        builder.Services.AddScoped<FileAccessPolicyRegistry>();
        builder.Services.AddSingleton<IFileScanner, NoOpFileScanner>();
        builder.Services.AddValidatorsFromAssembly(typeof(FilesModule).Assembly);
        
        // Default uploader-only policies for the built-in OwnerTypes. Owning modules register their
        // own policies for additional OwnerTypes via services.AddFileAccessPolicy<TPolicy>().
        builder.Services.AddScoped<IFileAccessPolicy>(_ => new DefaultUploaderOnlyPolicy("MyFiles"));
        builder.Services.AddScoped<IFileAccessPolicy>(_ => new DefaultUploaderOnlyPolicy("User"));
        
        builder.Services.AddHealthChecks().AddDbContextCheck<FilesDbContext>(
            name: "db:files",
            failureStatus: HealthStatus.Unhealthy);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}