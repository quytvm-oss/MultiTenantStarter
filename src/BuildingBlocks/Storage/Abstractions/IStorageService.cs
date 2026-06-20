using Shared.Storage;

using Storage.Constant;
using Storage.Dtos;

namespace Storage.Abstractions;

public interface IStorageService
{
    Task<string> UploadAsync<T>(
        StreamUploadRequest request,
        FileType fileType,
        CancellationToken cancellationToken = default) where T : class;
    
    Task<string> UploadAsync<T>(
        BufferedUploadRequest request,
        FileType fileType,
        CancellationToken cancellationToken = default) where T : class;

    Task<FileDownloadResponse?> DownloadAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string path, CancellationToken cancellationToken = default);
}