using Core.Context;

using Mediator;

using Microsoft.Extensions.Options;

using Modules.Files.Contracts.DTOs;

using Modules.Files.Contracts.v1.Commands;
using Modules.Files.Data;
using Modules.Files.Services;

using Storage.Abstractions;

namespace Modules.Files.Features.v1.RequestUploadUrl;

public class RequestUploadUrlCommandHandler : ICommandHandler<RequestUploadUrlCommand, PresignedUploadResponse>
{
    private readonly FilesDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly FileAccessPolicyRegistry _fileAccessPolicyRegistry;
    private readonly ICurrentUser _currentUser;

    private readonly IOptions<FilesOptions> options;


    public ValueTask<PresignedUploadResponse> Handle(RequestUploadUrlCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

}
