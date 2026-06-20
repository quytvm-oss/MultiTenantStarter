using Microsoft.AspNetCore.Builder;

namespace Shared.Identity.Authorization;

public static class EndpointExtensions
{
    public static TBuilder RequirePermission<TBuilder>(
        this TBuilder endpointConversationBuilder, string requiredPermission, params string[] additionalRequiredPermissions) 
        where TBuilder : IEndpointConventionBuilder
    {
        return endpointConversationBuilder.WithMetadata(
            new RequiredPermissionAttribute(requiredPermission, additionalRequiredPermissions));
    }
}