using System.Net;
using System.Text.RegularExpressions;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Storage;

using Storage.Abstractions;
using Storage.Constant;
using Storage.Dtos;

namespace Storage.S3;

internal sealed partial class S3StorageService : IStorageService
{
    private const string UploadBasePath = "uploads";
    private readonly IAmazonS3 _s3;
    private readonly ITransferUtility _transfer;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    
    public S3StorageService(
        IAmazonS3 s3,
        ITransferUtility transfer,
        IOptions<S3StorageOptions> options,
        ILogger<S3StorageService> logger)
    {
        _s3       = s3;
        _transfer = transfer;
        _options  = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger   = logger;
 
        if (string.IsNullOrWhiteSpace(_options.Bucket))
            throw new InvalidOperationException("Storage:S3:Bucket is required when using S3 storage.");
    }

    public string RootPath => string.Empty;

    public Task<string> UploadAsync<T>(FileUploadRequest request, FileType fileType, 
        CancellationToken cancellationToken = default) 
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        var stream = new MemoryStream(request.Data, writable: false);
        var streamRequest = new StreamUploadRequest
        {
            FileName    = request.FileName,
            ContentType = request.ContentType,
            Stream      = stream
        };
        return UploadAsync<T>(streamRequest, fileType, cancellationToken);
    }

    public async Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var key = NormalizeKey(path);
            var request = new GetObjectRequest()
            {
                BucketName = _options.Bucket,
                Key = key
            };
            
            // response is IDisposable — ownership is transferred to S3ResponseStream below.
            var response = await _s3.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

            var fileName = Path.GetFileName(key);
            var contentType = response.Headers.ContentType;

            if (string.IsNullOrWhiteSpace(contentType) &&
                !_contentTypeProvider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream";
            }

            return new FileDownloadResponse()
            {
                // S3ResponseStream disposes `response` when the stream is closed,
                // releasing the underlying HTTP connection automatically.
                Stream = new ResponseStream(response.ResponseStream, response),
                ContentType = contentType!,
                FileName = fileName,
                ContentLength = response.ContentLength
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("S3 object not found: {Path}", path);
 
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error downloading object {Path}: {StatusCode}", path, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error downloading S3 object {Path}", path);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        try
        {
            var key = NormalizeKey(path);
            var request = new GetObjectMetadataRequest() { BucketName = _options.Bucket, Key = key };
            
            await _s3.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            // 404 → object absent; 403 → no read permission (treat as absent for callers)
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "S3 error checking existence of {Path}: {StatusCode}", path, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error checking existence of S3 object {Path}", path);
            return false;
        }
    }

    public async Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var key = NormalizeKey(path);
            await _s3.DeleteObjectAsync(_options.Bucket, key, cancellationToken).ConfigureAwait(false);
            
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Deleted S3 object {Key} from bucket {Bucket}", key, _options.Bucket);
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

    public async Task<PresignedUploadUrl> GenerateUploadUrlAsync(string storageKey, string contentType, long maxBytes, TimeSpan ttl,
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

        var url = await _s3.GetPreSignedURLAsync(request).ConfigureAwait(false);

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

    public async Task<Uri> GenerateDownloadUrlAsync(string storageKey, TimeSpan ttl, string? responseContentDisposition,
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

        var url = await _s3.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new Uri(url);
    }

    public async Task<StoredObjectMetadata?> HeadObjectAsync(string storageKey, CancellationToken cancellationToken = default)
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

    public string BuildPublicUrl(string key)
    {
        var safeKey = key.TrimStart('/');
        
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{safeKey}";
        
        if (!_options.PublicRead)
        {
            var presignRequest = new GetPreSignedUrlRequest
            {
                BucketName = _options.Bucket,
                Key        = safeKey,
                Verb       = HttpVerb.GET,
                Expires    = DateTime.UtcNow.Add(_options.PresignedUrlExpiry)
            };
            return _s3.GetPreSignedURL(presignRequest);
        }
        
        return string.IsNullOrWhiteSpace(_options.Region) ||
               string.Equals(_options.Region, "us-east-1", StringComparison.OrdinalIgnoreCase)
            ? $"https://{_options.Bucket}.s3.amazonaws.com/{safeKey}"
            : $"https://{_options.Bucket}.s3.{_options.Region}.amazonaws.com/{safeKey}";
    }
    
    // MinIO and other S3-compatibles often serve plain HTTP, but the SDK defaults presigned URLs to
    // HTTPS regardless of ServiceURL scheme (un-PUTable there). Infer protocol from ServiceURL.
    private Protocol ResolvePresignProtocol()
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl)
            && Uri.TryCreate(_options.ServiceUrl, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return Protocol.HTTP;
        }
        return Protocol.HTTPS;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------
    private async Task<string> UploadAsync<T>(StreamUploadRequest request, FileType fileType, 
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Stream, nameof(request.Stream));
        
        var rules     = FileTypeMetadata.GetRules(fileType);
        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !rules.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Extension '{extension}' is not allowed. Allowed: {string.Join(", ", rules.AllowedExtensions)}");
        }
        
        // Validate size via stream length — no buffer copy needed.
        // Note: if the stream is not seekable (e.g. a raw network stream),
        // Length will throw; callers should enforce size upstream in that case.
        var maxBytes = rules.MaxSizeInMb * 1024L * 1024L;

        // Fast path: seekable stream — check before upload.
        if (request.Stream.CanSeek && request.Stream.Length > maxBytes)
            throw new InvalidOperationException($"File exceeds the maximum allowed size of {rules.MaxSizeInMb} MB.");
        
        var key = BuildKey<T>(SanitizeFileName(request.FileName));
        var uploadStream = request.Stream.CanSeek
            ? request.Stream
            : new MaxReadStream(request.Stream, maxBytes);

        var uploadRequest = new TransferUtilityUploadRequest()
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = uploadStream,
            ContentType = request.ContentType,
            PartSize = _options.MultipartPartSizeBytes,
            AutoCloseStream = false
        };
        
        await _transfer.UploadAsync(uploadRequest, cancellationToken).ConfigureAwait(false);
        
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Uploaded {Key} to S3 bucket {Bucket}", key, _options.Bucket);
        
        return BuildPublicUrl(key);
    }
    
    
    private string BuildKey<T>(string fileName) where T : class
    {
        var folder       = NonAlphanumericRegex().Replace(typeof(T).Name.ToLowerInvariant(), "_");
        var relativePath = $"{UploadBasePath}/{folder}/{Guid.NewGuid():N}_{fileName}";

        return string.IsNullOrWhiteSpace(_options.Prefix)
            ? relativePath
            : $"{_options.Prefix.TrimEnd('/')}/{relativePath}";
    }
    
    
    private string NormalizeKey(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            path = uri.AbsolutePath;
 
        var trimmed = path.TrimStart('/');
 
        if (string.IsNullOrWhiteSpace(_options.Prefix))
            return trimmed;
 
        var prefix = _options.Prefix.TrimEnd('/');
        
        return trimmed.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{prefix}/{trimmed}";
    }
 
    private static string SanitizeFileName(string fileName)
        => NonAlphanumericFileNameRegex().Replace(fileName, "_");
 
    // Source-generated regexes — compiled once, zero allocation at call site.
    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NonAlphanumericRegex();
 
    [GeneratedRegex(@"[^a-zA-Z0-9_.\-]")]
    private static partial Regex NonAlphanumericFileNameRegex();
}