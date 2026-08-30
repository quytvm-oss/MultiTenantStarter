using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Files.Contracts.Authorization;
using Modules.Files.Contracts.v1.Commands;

using Shared.Identity.Authorization;

using Web.Idempotency;

namespace Modules.Files.Features.v1.RequestUploadUrl;

public static class RequestUploadUrlEndpoint
{
    internal static RouteHandlerBuilder MapRequestUploadUrlEndpoint(this IEndpointRouteBuilder builder)
        => builder.MapPost("/upload-url",
                async (RequestUploadUrlCommand command, IMediator mediator, CancellationToken ct) =>
                    TypedResults.Ok(await mediator.Send(command, ct)))
            .WithName("RequestFileUploadUrl")
            .WithSummary("Mint a presigned PUT URL for a file upload")
            .RequirePermission(FilesPermissions.Upload)
            .WithIdempotency();
}