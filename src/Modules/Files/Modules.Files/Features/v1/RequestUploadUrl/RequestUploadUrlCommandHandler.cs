using System.Net;

using Core.Context;
using Core.Exceptions;

using Mediator;

using Microsoft.Extensions.Options;

using Modules.Files.Contracts.DTOs;

using Modules.Files.Contracts.v1.Commands;
using Modules.Files.Data;
using Modules.Files.Domain;
using Modules.Files.Services;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.RequestUploadUrl;

public class RequestUploadUrlCommandHandler : ICommandHandler<RequestUploadUrlCommand, PresignedUploadResponse>
{
    private readonly FilesDbContext _db;
    private readonly IStorageService _storageService;
    private readonly FileAccessPolicyRegistry _fileAccessPolicyRegistry;
    private readonly ICurrentUser _currentUser;
    private readonly FilesOptions _options;


    public RequestUploadUrlCommandHandler(
        FilesDbContext dbContext, 
        IStorageService storageService, 
        FileAccessPolicyRegistry fileAccessPolicyRegistry,
        ICurrentUser currentUser, 
        IOptions<FilesOptions> options)
    {
        _db = dbContext;
        _storageService = storageService;
        _fileAccessPolicyRegistry = fileAccessPolicyRegistry;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async ValueTask<PresignedUploadResponse> Handle(RequestUploadUrlCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        
        var tenantId = _currentUser.GetTenantId() ?? throw new UnauthorizedException("invalid tenant");
        var userId = _currentUser.GetUserId();
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("no current user");
        }
        
        // Category lookup + extension/size validation.
        if (!_options.Categories.TryGetValue(command.Category, out var category))
        {
            throw new CustomException($"Unknown category: '{command.Category}'.", (IEnumerable<string>?)null, HttpStatusCode.BadRequest);
        }
        
        var extension = Path.GetExtension(command.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !category.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new CustomException($"Extension '{extension}' not allowed for category '{command.Category}'.",
                (IEnumerable<string>?)null, HttpStatusCode.BadRequest);
        }

        if (command.SizeBytes > category.MaxBytes)
        {
            throw new CustomException($"File exceeds max size of {{category.MaxBytes}} bytes for category '{{cmd.Category}}'.",
                (IEnumerable<string>?)null, HttpStatusCode.BadRequest);
        }
        
        // Authorization: policy must exist and allow the attach.
        var policy = _fileAccessPolicyRegistry.Resolve(command.OwnerType)
                     ?? throw new ForbiddenException($"No file access policy registered for owner type '{command.OwnerType}'.");
        if (!await policy.CanAttachAsync(command.OwnerId, userId.ToString(), cancellationToken).ConfigureAwait(false))
        {
            throw new ForbiddenException("Not allowed to attach files to this owner.");
        }
        
        // Generate id + storage key + presigned URL.
        var id = Guid.CreateVersion7();
        var storageKey = StorageKeyBuilder.Build(tenantId, command.OwnerType, id, command.FileName, DateTimeOffset.UtcNow);
        var ttl = TimeSpan.FromMinutes(_options.UploadUrlTtlMinutes);
        var presigned = await _storageService.GenerateUploadUrlAsync(storageKey, command.ContentType, category.MaxBytes,
            ttl, cancellationToken).ConfigureAwait(false);
        
        var asset = FileAsset.CreatePending(
            id: id,
            ownerType: command.OwnerType,
            ownerId: command.OwnerId,
            originalFileName: command.FileName,
            sanitizedFileName: StorageKeyBuilder.Sanitize(command.FileName),
            contentType: command.ContentType,
            declaredSizeBytes: command.SizeBytes,
            storageKey: storageKey,
            visibility: command.Visibility,
            createdByUserId: userId.ToString(),
            uploadDeadline: DateTimeOffset.UtcNow.Add(ttl));

        await _db.FileAssets.AddAsync(asset, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        return new PresignedUploadResponse(asset.Id, presigned.Url, presigned.RequiredHeaders, presigned.ExpiresAt);
    }

}
