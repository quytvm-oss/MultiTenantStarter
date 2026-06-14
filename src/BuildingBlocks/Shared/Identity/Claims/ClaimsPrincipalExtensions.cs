using System.Security.Claims;

namespace Shared.Identity.Claims;

public static class ClaimsPrincipalExtensions
{
    // Retrieves the user's ID
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    
    public static string? GetTenant(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(CustomClaims.Tenant);

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.Email);
}