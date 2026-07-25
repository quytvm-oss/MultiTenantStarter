using System.Security.Claims;

using Core.Exceptions;

using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.UpdateUser;

using Shared.Identity.Claims;
using Shared.Storage;

namespace Modules.Identity.Features.v1.Users.UpdateUser;

public static class UpdateUserEndpoint
{
    internal static RouteHandlerBuilder MapUpdateUserEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/profile",
            async ([FromForm] UpdateUserProfileRequest request, ClaimsPrincipal user, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                if (user.GetUserId() is not { } userId ||
                    string.IsNullOrWhiteSpace(userId))
                {
                    throw new UnauthorizedException();
                }

                StreamUploadRequest? image = null;

                if (request.Image is not null)
                {
                    image = new StreamUploadRequest
                    {
                        FileName = request.Image.FileName,
                        ContentType = request.Image.ContentType,
                        Stream = request.Image.OpenReadStream()
                    };
                }

                var command = new UpdateUserCommand
                {
                    Id = userId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Image = image,
                    DeleteCurrentImage = request.DeleteCurrentImage
                };

                await mediator.Send(command, cancellationToken);

                return TypedResults.Ok();
            })
            .WithName("UpdateUserProfile")
            .WithSummary("Update user profile")
            .WithDescription(
                "Update profile details for the authenticated user. " +
                "Any signed-in user may edit their own profile; no admin permission required.")
            .Accepts<UpdateUserProfileRequest>("multipart/form-data")
            .DisableAntiforgery()
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest);
    }
}