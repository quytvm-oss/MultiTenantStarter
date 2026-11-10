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

public sealed partial class LocalStorageService : IStorageService
{
    // UploadAsync hands back a durable public URL, so objects live under the public root (same as S3StorageService).
    // Keys written before the root existed stay "uploads/…".
    private const string UploadBasePath = StorageVisibilityRoot.Public + "uploads";

    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    // Linux filesystems are case-sensitive, so the containment check must be too.
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    // Source-generated, compiled once — the inline Regex.Replace calls re-parsed the pattern on every upload.
    [GeneratedRegex("[^a-z0-9]")]
    private static partial Regex FolderSanitizer();

    [GeneratedRegex(@"[^a-zA-Z0-9_\.-]")]
    private static partial Regex FileNameSanitizer();

    // Dev-only presigning fallback when Storage:Provider != s3 (prod uses S3StorageService). Token
    // store is process-static so the dev middleware can consume the token without re-resolving DI.
    private static LocalPresignTokenStore? _staticTokenStore;
    public static LocalPresignTokenStore SharedTokenStore => LazyInitializer.EnsureInitialized(ref _staticTokenStore);

    public LocalStorageService(IWebHostEnvironment environment, IOptions<LocalStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);

        var configuredRoot = options.Value.StorageRoot?.Trim();

        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            configuredRoot = "Storage";
        }

        // Relative roots are resolved against the parent of the content root (the host folder),
        // so storage lives next to the app rather than inside it.
        var hostRoot = Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath;

        _rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(
            Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(hostRoot, configuredRoot)));

        _rootPathWithSeparator = _rootPath + Path.DirectorySeparatorChar;

        Directory.CreateDirectory(_rootPath);
    }

    public string RootPath => _rootPath;

    public async Task<string> UploadAsync<T>(FileUploadRequest request, FileType fileType, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);

        using var stream = new MemoryStream([.. request.Data], writable: false);
        var streamRequest = new StreamUploadRequest
        {
            FileName = request.FileName,
            ContentType = request.ContentType,
            Stream = stream
        };

        return await UploadCoreAsync<T>(streamRequest, fileType, cancellationToken).ConfigureAwait(false);
    }

    public Task<FileDownloadResponse?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(path, out var fullPath) || !File.Exists(fullPath))
        {
            return Task.FromResult<FileDownloadResponse?>(null);
        }

        var fileInfo = new FileInfo(fullPath);
        var fileName = Path.GetFileName(fullPath);

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

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
        return Task.FromResult(TryResolvePath(path, out var fullPath) && File.Exists(fullPath));
    }

    public Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(path, out var fullPath) || !File.Exists(fullPath))
        {
            return Task.FromResult(0L);
        }

        return Task.FromResult(new FileInfo(fullPath).Length);
    }

    public Task RemoveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (TryResolvePath(path, out var fullPath) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationKey);
        cancellationToken.ThrowIfCancellationRequested();

        // Both ends are validated: a key escaping the storage root throws UnauthorizedAccessException.
        var sourcePath = ResolveAndValidatePath(sourceKey);
        var destinationPath = ResolveAndValidatePath(destinationKey);

        // Callers delete the source after a copy, so a failed copy must not look like a successful one.
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Cannot copy '{sourceKey}': the object does not exist.", sourceKey);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return Task.CompletedTask;
    }

    public Task<PresignedUploadUrl> GenerateUploadUrlAsync(
        string storageKey,
        string contentType,
        long maxBytes,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var token = SharedTokenStore.Issue(storageKey, contentType, maxBytes, ttl);
        var url = new Uri($"local://upload/{token}", UriKind.Absolute);
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["Content-Type"] = contentType };
        return Task.FromResult(new PresignedUploadUrl(url, headers, DateTimeOffset.UtcNow.Add(ttl)));
    }

    public Task<Uri> GenerateDownloadUrlAsync(
        string storageKey,
        TimeSpan ttl,
        string? responseContentDisposition = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        // Local mode needs no signing: files are served as static files from RootPath.
        var normalized = storageKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
        return Task.FromResult(new Uri($"/{normalized}", UriKind.Relative));
    }

    public Task<StoredObjectMetadata?> HeadObjectAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(storageKey, out var fullPath) || !File.Exists(fullPath))
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

#pragma warning disable CA1055 // returns a server-relative path, not a well-formed Uri — see IStorageService.BuildPublicUrl
    public string BuildPublicUrl(string storageKey)
#pragma warning restore CA1055
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        var normalized = storageKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
        // Resolved later against the dashboard's API origin (as UserProfileService does for legacy
        // avatars). The leading slash lets clients distinguish absolute from server-relative URLs.
        return $"/{normalized}";
    }

    private async Task<string> UploadCoreAsync<T>(StreamUploadRequest request, FileType fileType, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Stream, nameof(request.Stream));

        var rules = FileTypeMetadata.GetRules(fileType);
        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !rules.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"File type '{extension}' is not allowed. Allowed: {string.Join(", ", rules.AllowedExtensions)}");
        }

        var maxBytes = rules.MaxSizeInMb * 1024L * 1024L;

        // Seekable stream: reject before touching the disk.
        if (request.Stream.CanSeek && request.Stream.Length > maxBytes)
        {
            throw new InvalidOperationException($"File exceeds max size of {rules.MaxSizeInMb} MB.");
        }

#pragma warning disable CA1308 // folder names are intentionally lower-case for URLs/paths
        var folder = FolderSanitizer().Replace(typeof(T).Name.ToLowerInvariant(), "_");
#pragma warning restore CA1308
        var safeFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(request.FileName)}";
        var relativePath = Path.Combine(UploadBasePath, folder, safeFileName);
        var fullPath = ResolveAndValidatePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var success = false;
        var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        try
        {
            var totalRead = 0L;
            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                int read;
                while ((read = await request.Stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    totalRead += read;
                    if (totalRead > maxBytes)
                    {
                        throw new InvalidOperationException($"File exceeds max size of {rules.MaxSizeInMb} MB.");
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
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
            await fileStream.DisposeAsync().ConfigureAwait(false);
            if (!success)
            {
                File.Delete(fullPath);
            }
        }

        return relativePath.Replace("\\", "/", StringComparison.Ordinal);
    }

    // Non-throwing variant for read/delete paths: an invalid or escaping key behaves like "not found".
    private bool TryResolvePath(string? path, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            fullPath = ResolveAndValidatePath(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private string ResolveAndValidatePath(string relativePath)
    {
        // TrimStart('/') so a leading slash (URL-style key) is treated as relative, not as a rooted path.
        var normalized = relativePath
            .TrimStart('/', '\\')
            .Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));

        if (!fullPath.StartsWith(_rootPathWithSeparator, PathComparison))
        {
            throw new UnauthorizedAccessException("Access outside storage root is not allowed.");
        }

        return fullPath;
    }

    private static string SanitizeFileName(string fileName)
    {
        return FileNameSanitizer().Replace(fileName, "_");
    }
}