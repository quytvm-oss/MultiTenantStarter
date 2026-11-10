using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddOptions<LocalStorageOptions>().BindConfiguration($"{SectionName}:Local")
            .Validate(o => !string.IsNullOrWhiteSpace(o.StorageRoot),
                "Storage:Local:StorageRoot is required when using Local storage.")
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddScoped<LocalStorageService>();
        RegisterStorageService<LocalStorageService>(services, IsQuotaEnabled(configuration), ServiceLifetime.Scoped);
        return services;
    }

    private static IServiceCollection AddHeroS3FileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<S3StorageOptions>().BindConfiguration($"{SectionName}:S3")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Bucket),
                "Storage:S3:Bucket is required when using S3 storage.")
            .Validate(o => o.MultipartPartSizeBytes >= 5 * 1024 * 1024,
                "Storage:S3:MultipartPartSizeBytes must be at least 5 MB.")
            .Validate(o => o.PresignedUrlExpiry > TimeSpan.Zero,
                "Storage:S3:PresignedUrlExpiry must be positive.")
            .Validate(o => string.IsNullOrWhiteSpace(o.PresignEndpoint) || IsRootHttpUrl(o.PresignEndpoint),
                "Storage:S3:PresignEndpoint must be an absolute http(s) URL with no path, e.g. https://s3.example.com.")
            .Validate(o => !PrefixStartsWithVisibilityRoot(o.Prefix),
                "Storage:S3:Prefix must not start with 'public' or 'private': keys are stored as {Prefix}/public/... and {Prefix}/private/..., and a public/* read policy would then cover private files.")
            .ValidateDataAnnotations().ValidateOnStart();

        // Internal client: all real I/O (put/get/delete/copy/head). Talks to ServiceUrl,
        // which may be a Docker-internal host such as http://minio:9000.
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;
            return CreateS3Client(options, options.ServiceUrl);
        });

        // A second client only so presigned URLs are signed for the host browsers use. Signing itself is
        // offline, and without explicit keys the only call this client makes is the one-time fetch of
        // ambient credentials. Falls back to ServiceUrl when PresignEndpoint is not set.
        services.AddKeyedSingleton<IAmazonS3>(S3StorageService.PresignClientKey, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;
            var presignUrl = string.IsNullOrWhiteSpace(options.PresignEndpoint)
                ? options.ServiceUrl
                : options.PresignEndpoint;
            return CreateS3Client(options, presignUrl);
        });

        // Multipart / streaming uploads for S3StorageService. Uses the internal client.
        services.AddSingleton<ITransferUtility>(sp =>
        {
            var s3 = sp.GetRequiredService<IAmazonS3>();
            var options = sp.GetRequiredService<IOptions<S3StorageOptions>>().Value;

            return new TransferUtility(s3, new TransferUtilityConfig
            {
                MinSizeBeforePartUpload = options.MultipartPartSizeBytes,
                ConcurrentServiceRequests = 4
            });
        });

        services.AddTransient<S3StorageService>();
        RegisterStorageService<S3StorageService>(services, IsQuotaEnabled(configuration), ServiceLifetime.Transient);
        return services;
    }

    private static bool IsQuotaEnabled(IConfiguration configuration) =>
        configuration.GetSection(nameof(QuotaOptions)).Get<QuotaOptions>()?.Enable == true;


    // serviceUrl is the endpoint this client talks to; null/empty means plain AWS (Region) or the SDK default.
    private static AmazonS3Client CreateS3Client(S3StorageOptions options, string? serviceUrl)
    {
        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            // S3-compatible endpoint (e.g. MinIO). Path-style addressing is typically required
            // because these services don't route virtual-hosted-style bucket subdomains.
            config.ServiceURL = serviceUrl;
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
    }

    // A path would be signed into every URL (https://host/s3/bucket/key): a proxy that strips it breaks
    // the signature, and one that keeps it makes the store read "s3" as the bucket.
    private static bool IsRootHttpUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.AbsolutePath == "/"
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment);

    // Keys are {Prefix}/{public|private}/…. A prefix whose first segment is itself a visibility root puts
    // private keys under public/ (or the other way round), where the bucket's public/* read policy applies.
    private static bool PrefixStartsWithVisibilityRoot(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        var firstSegment = prefix.Trim().TrimStart('/').Split('/', 2)[0];
        return string.Equals(firstSegment + "/", StorageVisibilityRoot.Public, StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstSegment + "/", StorageVisibilityRoot.Private, StringComparison.OrdinalIgnoreCase);
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