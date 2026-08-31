using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

using Modules.Files.Contracts;

using Modules.Files.Contracts.DTOs;

using Modules.Files.Contracts.v1.Queries;
using Modules.Files.Data;
using Modules.Files.Services;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.GetFileDownloadUrl;

public class GetFileDownloadUrlQueryHandler : IQueryHandler<GetFileDownloadUrlQuery, PresignedDownloadResponse>
{
    private readonly FilesDbContext _db;
    private readonly IStorageService _storage;
    private readonly ICurrentUser _current;
    private readonly FileAccessPolicyRegistry _policy;
    private readonly FilesOptions _options;

    public GetFileDownloadUrlQueryHandler(
        FilesDbContext db,
        IStorageService storage,
        ICurrentUser current,
        FileAccessPolicyRegistry policy,
        IOptions<FilesOptions> options)
    {
        _db = db;
        _storage = storage;
        _current = current;
        _policy = policy;
        _options = options.Value;
    }
    public async ValueTask<PresignedDownloadResponse> Handle(GetFileDownloadUrlQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var f = await _db.FileAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.FileAssetId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("file not found");

        var userId = _current.GetUserId().ToString();
        var policy = _policy.Resolve(f.OwnerType)
            ?? throw new NotFoundException("file not found");

        var ctx = new FileAccessContext(f.Id, f.OwnerType, f.OwnerId, f.CreatedByUserId, (int)f.Visibility);
        if (!await policy.CanReadAsync(ctx, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("file not found");
        }

        var ttl = TimeSpan.FromMinutes(_options.DownloadUrlTtlMinutes);
        var mode = query.Inline ? "inline" : "attachment";
        var disposition = $"{mode}; filename=\"{f.OriginalFileName}\"";
        var url = await _storage.GenerateDownloadUrlAsync(f.StorageKey, ttl, disposition, cancellationToken).ConfigureAwait(false);
        return new PresignedDownloadResponse(url, DateTimeOffset.UtcNow.Add(ttl));
    }

}
