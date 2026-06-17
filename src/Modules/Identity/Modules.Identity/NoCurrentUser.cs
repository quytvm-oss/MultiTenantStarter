using System.Security.Claims;

using Core.Context;

namespace Modules.Identity;

public class NoCurrentUser : ICurrentUser
{
    public string? Name { get; set; }
    public Guid GetUserId()
    {
       return Guid.Empty;
    }

    public string? GetUserEmail()
    {
        return string.Empty;
    }

    public string? GetTenantId()
    {
        return string.Empty;
    }

    public bool IsAuthenticated()
    {
        return false;
    }

    public bool IsInRole(string role)
    {
        return false;
    }

    public IEnumerable<Claim>? GetUserClaims()
    {
       return Enumerable.Empty<Claim>();
    }
}