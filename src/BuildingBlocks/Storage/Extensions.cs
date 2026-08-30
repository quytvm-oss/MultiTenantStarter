using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Quota;

using Shared.Multitenancy;

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

            if (string.IsNullOrWhiteSpace(options.Bucket))
            {
                throw new InvalidOperationException("Storage:S3:Bucket is required when using S3 storage.");
            }

            var config = new AmazonS3Config();

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                // S3-compatible endpoint (e.g. MinIO). Path-style addressing is typically required
                // because these services don't route virtual-hosted-style bucket subdomains.
                config.ServiceURL = options.ServiceUrl;
                config.ForcePathStyle = options.ForcePathStyle;

                // The SDK still wants an auth region for SigV4 even when hitting a custom endpoint.
                config.AuthenticationRegion = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region;
            }
            else if (!string.IsNullOrWhiteSpace(options.Region))
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
            }

            var hasExplicitCredentials = !string.IsNullOrWhiteSpace(options.AccessKey)
                                         && !string.IsNullOrWhiteSpace(options.SecretAccessKey);

            return hasExplicitCredentials
                ? new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretAccessKey), config)
                : new AmazonS3Client(config);
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

    private static void RegisterStorageService<TInner>(
        this IServiceCollection services,
        bool quotaEnabled,
        ServiceLifetime lifetime) where TInner : class, IStorageService
    {
        if (quotaEnabled)
        {
            services.AddScoped<IStorageService>(sp =>
                new QuotaMeteredStorageService(
                    sp.GetRequiredService<TInner>(),
                    sp.GetRequiredService<IQuotaService>(),
                    sp.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>(),
                    sp.GetRequiredService<ILogger<QuotaMeteredStorageService>>()));

            return;
        }

        
        services.Add(new ServiceDescriptor(
            typeof(IStorageService),
            sp => sp.GetRequiredService<IStorageService>(),
            lifetime));
    }
}