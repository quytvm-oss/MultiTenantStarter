using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Modules.Identity.Helpers;

public static class ClaimsExtensions
{
    public static List<Claim> WithSession(this IEnumerable<Claim> claims, Guid sessionId)
    {
        var list = claims.Where(c => c.Type != JwtRegisteredClaimNames.Sid).ToList();
        list.Add(new Claim(JwtRegisteredClaimNames.Sid, sessionId.ToString()));
        return list;
    }

    public static Guid? GetSessionId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sid), out var id) ? id : null;
}