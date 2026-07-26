using Mediator;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Modules.Identity.Contracts.Authorization;
using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.v1.Users.SearchUsers;

using Shared.Identity.Authorization;
using Shared.Persistence;

namespace Modules.Identity.Features.v1.Users.SearchUsers;

public static class SearchUsersEndpoint
{
    internal static RouteHandlerBuilder MapSearchUsersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users/search",
            async ([AsParameters] SearchUsersQuery query, IMediator mediator, CancellationToken ct) =>
             TypedResults.Ok(await mediator.Send(query, ct)))
            .WithTags("SearchUsers")
            .WithSummary("Search users with pagination")
            .WithDescription("Search and filter users with server-side pagination, sorting, and filtering by status, email confirmation, and role.")
            .RequirePermission(IdentityPermissions.Users.View)
            .Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}