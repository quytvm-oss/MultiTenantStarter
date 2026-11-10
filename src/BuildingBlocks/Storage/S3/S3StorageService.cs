using System.Net;
using System.Text.RegularExpressions;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Storage;

using Storage.Abstractions;
using Storage.Constant;
using Storage.Dtos;

namespace Storage.S3;

internal sealed partial class S3StorageService : IStorageService
{
    internal const string PresignClientKey = "Storage.S3.Presign";

    private readonly IAmazonS3 _s3;
    private readonly IAmazonS3 _presignS3;
    private readonly ITransferUtility _transfer;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    // UploadAsync always hands back a durable public URL, so these objects live under the public root
    // that bucket policies open to anonymous reads. Keys written before the root existed stay "uploads/…".
    private const string UploadBasePath = StorageVisibilityRoot.Public + "uploads";

    // Source-generated, compiled once — the inline Regex.Replace calls re-parsed the pattern on every upload.
    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex FolderSanitizer();

    [GeneratedRegex(@"[^a-zA-Z0-9_\.-]")]
    private static partial Regex FileNameSanitizer();

    public S3StorageService(
        IAmazonS3 s3,
        [FromKeyedServices(PresignClientKey)] IAmazonS3 presignS3,
        ITransferUtility transfer,
        IOptions<S3StorageOptions> options,
        ILogger<S3StorageService> logger)
    {
        _s3 = s3;
        _presignS3 = presignS3;
        _transfer = transfer;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.Bucket))
        {
            throw new InvalidOperationException("Storage:S3:Bucket is required when using S3 storage.");
        }
    }

    public string RootPath => string.Empty;

    public Task<string> UploadAsync<T>(FileUploadRequest request, FileType fileType, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        // Not disposed here: UploadCoreAsync runs to completion before the caller's await returns,
        // and MemoryStream holds no unmanaged resources.
        var stream = new MemoryStream([.. request.Data], writable: false);

        var streamRequest = new StreamUploadRequest
        {
            FileName = request.FileName,
            ContentType = request.ContentType,
            Stream = stream
        };

        return UploadCoreAsync<T>(streamRequest, fileType, cancellationToken);
    }

    private async Task<string> UploadCoreAsync<T>(StreamUploadRequest request, FileType fileType, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Stream, nameof(request.Stream));

        var rules = FileTypeMetadata.GetRules(fileType);
        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !rules.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"File type '{extension}' is not allowed. Allowed: {string.Join(", ", rules.AllowedExtensions)}");
        }

        var maxBytes = rules.MaxSizeInMb * 1024L * 1024L;

        // Seekable stream: check the size up front, no buffer copy needed.
        if (request.Stream.CanSeek && request.Stream.Length > maxBytes)
        {
            throw new InvalidOperationException($"File exceeds max size of {rules.MaxSizeInMb} MB.");
        }

        var key = BuildKey<T>(SanitizeFileName(request.FileName));

        // Non-seekable stream: MaxReadStream aborts once more than maxBytes have been read.
        var uploadStream = request.Stream.CanSeek
            ? request.Stream
            : new MaxReadStream(request.Stream, maxBytes);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = uploadStream,
            ContentType = request.ContentType,
            PartSize = _options.MultipartPartSizeBytes,
            AutoCloseStream = false
        };

        // Rely on bucket policy for public access; do not set ACLs to avoid conflicts with ACL-disabled buckets.
        await _transfer.UploadAsync(uploadRequest, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Uploaded file to S3 bucket {Bucket} with key {Key}", _options.Bucket, key);
        }

        return BuildPublicUrl(key);
    }

    public async Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var key = NormalizeKey(path);
            await _s3.DeleteObjectAsync(_options.Bucket, key, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error deleting object {Path}: {StatusCode}", path, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error deleting S3 object {Path}", path);
        }
    }

    public async Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationKey);

        var source = NormalizeKey(sourceKey);
        var destination = NormalizeKey(destinationKey);

        // CopyObject keeps Content-Type and user metadata (MetadataDirective COPY is the default).
        await _s3.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = _options.Bucket,
            SourceKey = source,
            DestinationBucket = _options.Bucket,
            DestinationKey = destination
        }, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Copied S3 object in bucket {Bucket} from {SourceKey} to {DestinationKey}",
                _options.Bucket, source, destination);
        }
    }

    public async Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var key = NormalizeKey(path);
            var request = new GetObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key
            };

            // response is IDisposable — ownership is transferred to ResponseStream below.
            var response = await _s3.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);
            var fileName = Path.GetFileName(key);

            // Use response ContentType if available, otherwise determine from extension
            var contentType = response.Headers.ContentType;
            if (string.IsNullOrWhiteSpace(contentType) && !_contentTypeProvider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            return new FileDownloadResponse
            {
                // ResponseStream disposes `response` when the stream is closed,
                // releasing the underlying HTTP connection.
                Stream = new ResponseStream(response.ResponseStream, response),
                ContentType = contentType!,
                FileName = fileName,
                ContentLength = response.ContentLength
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(ex, "S3 object not found: {Path}", path);
            }
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error downloading object {Path}: {StatusCode}", path, ex.StatusCode);
            return null;
        }
        // Fallback for unexpected non-S3 errors (e.g., network, serialization).
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error downloading S3 object {Path}", path);
            return null;
        }
    }

    public async Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        try
        {
            var key = NormalizeKey(path);
            var metadata = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken).ConfigureAwait(false);

            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return 0;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error reading object size {Path}: {StatusCode}", path, ex.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error reading S3 object size: {Path}", path);
            return 0;
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var key = NormalizeKey(path);
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key
            };

            await _s3.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error checking object existence {Path}: {StatusCode}", path, ex.StatusCode);
            return false;
        }
        // Fallback for unexpected non-S3 errors (e.g., network, configuration).
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error checking if S3 object exists: {Path}", path);
            return false;
        }
    }

    public async Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey,
        string contentType,
        long maxBytes,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var key = NormalizeKey(storageKey);
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
            Protocol = ResolvePresignProtocol()
        };

        var url = await _presignS3.GetPreSignedURLAsync(request).ConfigureAwait(false);

        var requiredHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Content-Type"] = contentType
        };

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Issued presigned PUT URL for bucket {Bucket} key {Key} expires {ExpiresAt}",
                _options.Bucket, key, expiresAt);
        }

        return new PresignedUploadUrl(new Uri(url), requiredHeaders, expiresAt);
    }

    public async Task<Uri> GenerateDownloadUrlAsync(
        string storageKey,
        TimeSpan ttl,
        string? responseContentDisposition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var key = NormalizeKey(storageKey);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = ResolvePresignProtocol()
        };

        if (!string.IsNullOrWhiteSpace(responseContentDisposition))
        {
            request.ResponseHeaderOverrides.ContentDisposition = responseContentDisposition;
        }

        var url = await _presignS3.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new Uri(url);
    }

    public async Task<StoredObjectMetadata?> HeadObjectAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        try
        {
            var key = NormalizeKey(storageKey);
            var metadata = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken).ConfigureAwait(false);

            var contentType = string.IsNullOrWhiteSpace(metadata.Headers.ContentType)
                ? "application/octet-stream"
                : metadata.Headers.ContentType;

            var lastModified = metadata.LastModified.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(metadata.LastModified.Value, DateTimeKind.Utc), TimeSpan.Zero)
                : DateTimeOffset.UtcNow;

            return new StoredObjectMetadata(
                metadata.ContentLength,
                contentType,
                lastModified,
                metadata.ETag);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 HEAD failed for {Key}: {StatusCode}", storageKey, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error on S3 HEAD for {Key}", storageKey);
            return null;
        }
    }

    // MinIO and other S3-compatibles often serve plain HTTP, but the SDK defaults presigned URLs to
    // HTTPS regardless of ServiceURL scheme (un-PUTable there). Infer protocol from the presign endpoint.
    private Protocol ResolvePresignProtocol()
    {
        // Same fallback as the keyed presign client in DI: PresignEndpoint if set, otherwise ServiceUrl.
        var endpoint = string.IsNullOrWhiteSpace(_options.PresignEndpoint) ? _options.ServiceUrl : _options.PresignEndpoint;
        if (!string.IsNullOrWhiteSpace(endpoint)
            && Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return Protocol.HTTP;
        }
        return Protocol.HTTPS;
    }

    private string BuildKey<T>(string fileName) where T : class
    {
        var folder = FolderSanitizer().Replace(typeof(T).Name.ToLowerInvariant(), "_");
        var relativePath = $"{UploadBasePath}/{folder}/{Guid.NewGuid():N}_{fileName}";

        return string.IsNullOrWhiteSpace(_options.Prefix)
            ? relativePath
            : $"{_options.Prefix.TrimEnd('/')}/{relativePath}";
    }

    public string BuildPublicUrl(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var key = NormalizeKey(storageKey);
        var safeKey = key.TrimStart('/');

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{safeKey}";
        }

        // S3-compatible endpoint with path-style addressing (MinIO and friends).
        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            return $"{_options.ServiceUrl.TrimEnd('/')}/{_options.Bucket}/{safeKey}";
        }

        if (!_options.PublicRead)
        {
            return key;
        }

        if (string.IsNullOrWhiteSpace(_options.Region) || string.Equals(_options.Region, "us-east-1", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://{_options.Bucket}.s3.amazonaws.com/{safeKey}";
        }

        return $"https://{_options.Bucket}.s3.{_options.Region}.amazonaws.com/{safeKey}";
    }

    private string NormalizeKey(string path)
    {
        // If a full URL was passed, strip host and query to get the object key.
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath;
        }

        var trimmed = path.TrimStart('/');

        if (string.IsNullOrWhiteSpace(_options.Prefix))
        {
            return trimmed;
        }

        // Compare with a trailing "/" so prefix "fsh" does not match "fshfoo/...".
        var prefix = _options.Prefix.TrimEnd('/');

        return trimmed.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{prefix}/{trimmed}";
    }

    private static string SanitizeFileName(string fileName)
    {
        return FileNameSanitizer().Replace(fileName, "_");
    }
}