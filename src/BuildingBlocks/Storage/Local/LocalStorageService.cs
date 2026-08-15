using System.Buffers;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

using Shared.Storage;

using Storage.Abstractions;
using Storage.Constant;
using Storage.Dtos;

namespace Storage.Local;

internal class LocalStorageService : IStorageService
{
    private const string UploadBasePath = "uploads";
    private readonly string _rootPath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    
    
    // Dev-only presigning fallback when Storage:Provider != s3 (prod uses S3StorageService). Token
    // store is process-static so the dev middleware can consume the token without re-resolving DI.
    private static LocalPresignTokenStore? _staticTokenStore;
    public static LocalPresignTokenStore SharedTokenStore => LazyInitializer.EnsureInitialized(ref _staticTokenStore);
    

    // public LocalStorageService(IWebHostEnvironment environment, IOptions<LocalStorageOptions> options)
    // {
    //     ArgumentNullException.ThrowIfNull(environment);
    //     ArgumentNullException.ThrowIfNull(options);
    //
    //     var configuredRoot = options.Value.StorageRoot?.Trim();
    //
    //     if (string.IsNullOrWhiteSpace(configuredRoot))
    //     {
    //         configuredRoot = "storage";
    //     }
    //
    //     _rootPath = Path.GetFullPath(
    //         Path.IsPathRooted(configuredRoot)
    //             ? configuredRoot
    //             : Path.Combine(environment.ContentRootPath, configuredRoot));
    //
    //     Directory.CreateDirectory(_rootPath);
    // }
    
    public LocalStorageService(IWebHostEnvironment environment, IOptions<LocalStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);

        var configuredRoot = options.Value.StorageRoot?.Trim();

        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            configuredRoot = "Storage";
        }

        var hostRoot = Directory.GetParent(environment.ContentRootPath)!.FullName;

        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(hostRoot, configuredRoot));

        Directory.CreateDirectory(_rootPath);
    }
    
    public string RootPath => _rootPath;

    public async Task<string> UploadAsync<T>(StreamUploadRequest request, FileType fileType,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var rules = FileTypeMetadata.GetRules(fileType);
        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !rules.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"File type '{extension}' is not allowed. Allowed: {string.Join(", ", rules.AllowedExtensions)}");
        }

        if (request.Stream.CanSeek && request.Stream.Length > rules.MaxSizeInMb * 1024L * 1024L)
        {
            throw new InvalidOperationException($"File exceeds the maximum allowed size of {rules.MaxSizeInMb} MB.");
        }


        var folder = Regex.Replace(typeof(T).Name.ToLowerInvariant(), @"[^a-z0-9]", "_");
        var safeFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(request.FileName)}";
        var relativePath = Path.Combine(UploadBasePath, folder, safeFileName);
        var fullPath = ResolveAndValidatePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var success = false;
        var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        try
        {
            var maxBytes = rules.MaxSizeInMb * 1024L * 1024L;
            var totalRead = 0L;
            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                int read;
                while ((read = await request.Stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    totalRead += read;
                    if (totalRead > maxBytes)
                        throw new InvalidOperationException($"File exceeds max size of {rules.MaxSizeInMb} MB.");

                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            success = true;
        }
        finally
        {
            await fileStream.DisposeAsync();
            if (!success) File.Delete(fullPath);
        }

        return relativePath.Replace("\\", "/", StringComparison.Ordinal);
    }

    public async Task<string> UploadAsync<T>(BufferedUploadRequest request, FileType fileType, 
        CancellationToken cancellationToken = default) 
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        using var stream = new MemoryStream(request.Data, writable: false);
        var streamRequest = new StreamUploadRequest
        {
            FileName    = request.FileName,
            ContentType = request.ContentType,
            Stream      = stream
        };
        return await  UploadAsync<T>(streamRequest, fileType, cancellationToken);
    }

    public Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult<FileDownloadResponse?>(null);

        string fullPath;
        try { fullPath = ResolveAndValidatePath(path); }
        catch (UnauthorizedAccessException) { return Task.FromResult<FileDownloadResponse?>(null); }

        if (!File.Exists(fullPath))
            return Task.FromResult<FileDownloadResponse?>(null);

        var fileInfo = new FileInfo(fullPath);
        var fileName = Path.GetFileName(fullPath);

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
            contentType = "application/octet-stream";

        FileStream? stream = null;
        try
        {
            stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            return Task.FromResult<FileDownloadResponse?>(new FileDownloadResponse
            {
                Stream = stream,
                ContentType = contentType,
                FileName = fileName,
                ContentLength = fileInfo.Length
            });
        }
        catch
        {
            stream?.Dispose();
            return Task.FromResult<FileDownloadResponse?>(null);
        }
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        try
        {
            var fullPath = ResolveAndValidatePath(path);
            return Task.FromResult(File.Exists(fullPath));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    public Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.CompletedTask;

        try
        {
            var fullPath = ResolveAndValidatePath(path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (UnauthorizedAccessException) { }

        return Task.CompletedTask;
    }

    public Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult<long>(0);
        }
        
        var normalizedPath = path.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<long>(0);
        }
        
        return Task.FromResult(new FileInfo(fullPath).Length);
    }

    public Task<PresignedUploadUrl> GenerateUploadUrlAsync(string storageKey, string contentType, long maxBytes, TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var token = SharedTokenStore.Issue(storageKey, contentType, maxBytes, ttl);
        var url = new Uri($"local://upload/{token}", UriKind.Absolute);
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["Content-Type"] = contentType };
        return Task.FromResult(new PresignedUploadUrl(url, headers, DateTimeOffset.UtcNow.Add(ttl)));
    }

    public Task<Uri> GenerateDownloadUrlAsync(string storageKey, TimeSpan ttl, string? responseContentDisposition,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        // Local mode serves files from /wwwroot — no signing required.
        var normalized = storageKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
        return Task.FromResult(new Uri($"/{normalized}", UriKind.Relative));
    }

    public Task<StoredObjectMetadata?> HeadObjectAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult<StoredObjectMetadata?>(null);
        }
        
        var normalizedPath = storageKey.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.Combine(_rootPath, normalizedPath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<StoredObjectMetadata?>(null);
        }

        var info = new FileInfo(fullPath);
        if (!_contentTypeProvider.TryGetContentType(info.Name, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return Task.FromResult<StoredObjectMetadata?>(new StoredObjectMetadata(
            info.Length,
            contentType!,
            new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            ETag: null));
    }

    public string BuildPublicUrl(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var normalized = storageKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
        // Resolved later against the dashboard's API origin (as UserProfileService does for legacy
        // avatars). The leading slash lets clients distinguish absolute from server-relative URLs.
        return $"/{normalized}";
    }

    private string ResolveAndValidatePath(string relativePath)
    {
        var normalized = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));

        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Access outside storage root is not allowed.");

        return fullPath;
    }

    private static string SanitizeFileName(string fileName)
        => Regex.Replace(fileName, @"[^a-zA-Z0-9_\.-]", "_");
}