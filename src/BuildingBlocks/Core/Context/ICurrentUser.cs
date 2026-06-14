using System.Security.Claims;

namespace Core.Context;

/// <summary>
/// Represents the current user context and provides methods to access information about the authenticated user and their associated properties.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets or sets the name of the current user.
    /// </summary>
    string? Name { get; set; }

    /// <summary>
    /// Retrieves the unique identifier of the current user.
    /// </summary>
    /// <returns>A <see cref="Guid"/> representing the user's unique identifier, or an empty value if the user is not authenticated.</returns>
    Guid GetUserId();

    /// <summary>
    /// Retrieves the email address of the current user.
    /// </summary>
    /// <returns>A string containing the user's email address, or null if the user does not have an email address or is not authenticated.</returns>
    string? GetUserEmail();

    /// <summary>
    /// Retrieves the unique identifier of the current tenant associated with the user.
    /// </summary>
    /// <returns>A string representing the tenant's unique identifier, or null if the tenant information is not available or the user is not authenticated.</returns>
    string? GetTenantId();

    /// <summary>
    /// Determines whether the current user is authenticated.
    /// </summary>
    /// <returns>A boolean value indicating whether the user is authenticated.</returns>
    bool IsAuthenticated();

    /// <summary>
    /// Determines whether the current user belongs to the specified role.
    /// </summary>
    /// <param name="role">The name of the role to check.</param>
    /// <returns>A boolean value indicating whether the user is in the specified role.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Retrieves the claims associated with the current user.
    /// </summary>
    /// <returns>A collection of <see cref="Claim"/> objects representing the claims of the current user, or null if the user is not authenticated or claims are unavailable.</returns>
    IEnumerable<Claim>? GetUserClaims();
}