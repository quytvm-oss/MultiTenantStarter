using System.Security.Claims;

using Core.Exceptions;

using Modules.Identity.Contracts.Services;

using Shared.Identity.Claims;

namespace Modules.Identity.Services;

public class CurrentUserService : ICurrentUserService
{
    private ClaimsPrincipal? _user;
    
    public string? Name => _user?.Identity?.Name;
    
    private Guid _userId = Guid.Empty;
    
    public Guid GetUserId()
    {
        return IsAuthenticated() ?
            Guid.Parse(_user?.GetUserId() ?? Guid.Empty.ToString()) : _userId;
    }

    public string? GetUserEmail()
    => IsAuthenticated() ? _user!.GetEmail() : string.Empty;

    public string? GetTenantId()
    => IsAuthenticated() ? _user!.GetTenant() : string.Empty;

    public bool IsAuthenticated()
    => _user?.Identity?.IsAuthenticated is true;

    public bool IsInRole(string role)
    => _user?.IsInRole(role) is true;

    public IEnumerable<Claim>? GetUserClaims()
    => _user?.Claims;

    public void SetCurrentUser(ClaimsPrincipal user)
    {
        if (_user != null)
        {
            throw new CustomException("Method reserved for in-scope initialization");
        }

        _user = user;
    }

    public void SetCurrentUserId(string userId)
    {
        if (_userId != Guid.Empty)
        {
            throw new CustomException("Method reserved for in-scope initialization");
        }

        if (!string.IsNullOrEmpty(userId))
        {
            _userId = Guid.Parse(userId);
        }
    }
}