using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using Storage;
using Storage.Abstractions;

namespace Web.Origin;

public static class Extensions
{
    public static IApplicationBuilder UseFileStorageStaticContent(this WebApplication app)
    {
        var options = app.Services
            .GetRequiredService<IOptions<StorageOptions>>()
            .Value;

        if (!string.Equals(options.Provider?.Trim(), "local", StringComparison.OrdinalIgnoreCase))
        {
            return app;
        }

        var localStorage = app.Services.GetRequiredService<IStorageService>();
        var originOptions = app.Services.GetRequiredService<IOptions<OriginOptions>>().Value;

        if (originOptions.StaticContentPath != null)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(localStorage.RootPath),
                RequestPath = originOptions.StaticContentPath
            });
        }

        return app;
    }
}