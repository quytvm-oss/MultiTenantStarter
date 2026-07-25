using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using Storage.Abstractions;
using Storage.Local;
using Storage.S3;

namespace Storage;

public static class Extensions
{
    private const string SectionName = "Storage";
    public static IServiceCollection AddHeroStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>().BindConfiguration(SectionName)
            .ValidateDataAnnotations().ValidateOnStart();

        var storageOptions = configuration.GetSection(SectionName).Get<StorageOptions>() ?? new StorageOptions();

        switch (storageOptions.Provider?.Trim().ToLowerInvariant())
        {
            case "local":
                services.AddHeroLocalFileStorage(configuration);
                break;
            case "s3":
                services.AddHeroS3FileStorage(configuration);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported storage provider: '{storageOptions.Provider}'. Allowed: Local, S3.");
        }
        return services;
    }
    
    private static IServiceCollection AddHeroLocalFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalStorageOptions>().BindConfiguration("Storage:Local")
            .Validate(o => !string.IsNullOrWhiteSpace(o.StorageRoot),
                "Storage:Local:StorageRoot is required when using Local storage.")
            .ValidateDataAnnotations().ValidateOnStart();
        
        services.AddSingleton<IStorageService, LocalStorageService>();
        return services;
    }

    private static IServiceCollection AddHeroS3FileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<S3StorageOptions>().BindConfiguration("Storage:S3")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Bucket),
                "Storage:S3:Bucket is required when using S3 storage.")
            .Validate(o => o.MultipartPartSizeBytes >= 5 * 1024 * 1024,
                "Storage:S3:MultipartPartSizeBytes must be at least 5 MB.")
            .Validate(o => o.PresignedUrlExpiry > TimeSpan.Zero,
                "Storage:S3:PresignedUrlExpiry must be positive.")
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Region))
            {
                return new AmazonS3Client();
            }

            return new AmazonS3Client(RegionEndpoint.GetBySystemName(options.Region));
        });
        
        services.AddSingleton<ITransferUtility>(sp =>
        {
            var s3 = sp.GetRequiredService<IAmazonS3>();
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;

            return new TransferUtility(s3, new TransferUtilityConfig
            {
                MinSizeBeforePartUpload   = options.MultipartPartSizeBytes,
                ConcurrentServiceRequests = 4
            });
        });

        services.AddSingleton<IStorageService, S3StorageService>();
        return services;
    }
    
    public static IApplicationBuilder UseFileStorageStaticContent(this WebApplication app)
    {
        var options = app.Services
            .GetRequiredService<IOptions<StorageOptions>>()
            .Value;

        if (!string.Equals(
                options.Provider?.Trim(),
                "local",
                StringComparison.OrdinalIgnoreCase))
        {
            return app;
        }

        var localStorage = app.Services
            .GetRequiredService<IStorageService>();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(localStorage.RootPath),
            RequestPath = "/api/v1/static-contents"
        });

        return app;
    }
}