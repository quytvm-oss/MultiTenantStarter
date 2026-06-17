using System.Security.Claims;

using Core.Context;

namespace Modules.Identity;

public class NoCurrentUserInitializer : ICurrentUserInitializer
{
    public void SetCurrentUser(ClaimsPrincipal user)
    {
    }

    public void SetCurrentUserId(string userId)
    {
    }
}