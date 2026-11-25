using Microsoft.AspNetCore.Http;

namespace Modules.Identity.Helpers;

public static class AuthCookieExtensions
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";
    private const string RefreshPath = "/api/v1/identity/token/refresh";
    private const string LogoutPath = "/api/v1/identity/logout";

    private static CookieOptions Options(DateTimeOffset? expires, string path) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = path,
        Expires = expires
    };

    public static void SetAuthCookies(this HttpResponse response, string accessToken, DateTime accessTokenExpiresAt,
        string refreshToken,
        DateTime refreshTokenExpiresAt)
    {
        response.Cookies.Append(AccessTokenCookie, accessToken,
            Options(accessTokenExpiresAt, "/"));

        response.Cookies.Append(RefreshTokenCookie, refreshToken,
            Options(refreshTokenExpiresAt, RefreshPath));
            
        response.Cookies.Append(RefreshTokenCookie, refreshToken,
            Options(refreshTokenExpiresAt, LogoutPath));
    }

    public static void ClearAuthCookies(this HttpResponse response)
    {
        // Delete Path/Secure/SameSite
        response.Cookies.Delete(AccessTokenCookie, Options(null, "/"));
        response.Cookies.Delete(RefreshTokenCookie, Options(null, RefreshPath));
        response.Cookies.Delete(RefreshTokenCookie, Options(null, LogoutPath));
    }

    public static string? GetRefreshToken(this HttpRequest request)
        => request.Cookies.TryGetValue(RefreshTokenCookie, out var v) && !string.IsNullOrEmpty(v) ? v : null;
}