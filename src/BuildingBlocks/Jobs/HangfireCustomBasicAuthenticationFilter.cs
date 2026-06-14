using System.Net.Http.Headers;
using System.Security.Cryptography;

using Hangfire.Dashboard;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Jobs;

public sealed class HangfireCustomBasicAuthenticationFilter : IDashboardAuthorizationFilter
{
    private const string AuthenticationScheme = "Basic";
    private readonly ILogger<HangfireCustomBasicAuthenticationFilter> _logger;
    private readonly HangfireOptions _options;

    public HangfireCustomBasicAuthenticationFilter(
        ILogger<HangfireCustomBasicAuthenticationFilter> logger,
        IOptions<HangfireOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers.Authorization!;

        if (MissingAuthorizationHeader(header))
        {
            _logger.LogInformation("Request is missing Authorization Header");
            SetChallengeResponse(httpContext);
            return false;
        }
        
        var authValues = AuthenticationHeaderValue.Parse(header!);
        
        if (NoBasicAuthentication(authValues))
        {
            _logger.LogInformation("Request is NOT BASIC authentication");
            SetChallengeResponse(httpContext);
            return false;
        }
        
        var tokens = ExtractAuthenticationTokens(authValues);

        if (tokens.AreInvalid())
        {
            _logger.LogInformation("Request is missing or invalid BASIC authentication tokens");
            SetChallengeResponse(httpContext);
            return false;
        }

        if (tokens.CredentialsMatch(_options.UserName, _options.Password))
        {
            _logger.LogInformation("Awesome, authentication tokens match configuration!");
            return true;
        }
        
        _logger.LogInformation("Hangfire dashboard authentication failed — credentials do not match configuration");

        SetChallengeResponse(httpContext);
        return false;
    }

    private BasicAuthenticationTokens ExtractAuthenticationTokens(AuthenticationHeaderValue authValues)
    {
        string? parameter = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter!));
        string[]? parts = parameter.Split(':');
        return new BasicAuthenticationTokens(parts);
    }

    private bool NoBasicAuthentication(AuthenticationHeaderValue authValues)
    {
        return !AuthenticationScheme.Equals(authValues.Scheme, StringComparison.OrdinalIgnoreCase);
    }

    private void SetChallengeResponse(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }

    private bool MissingAuthorizationHeader(StringValues header)
    {
        return string.IsNullOrWhiteSpace(header);
    }
}

internal class BasicAuthenticationTokens
{private readonly string[] _tokens;

    public string? Username => _tokens.Length > 0 ? _tokens[0] : null;
    public string? Password => _tokens.Length > 1 ? _tokens[1] : null;

    public BasicAuthenticationTokens(string[] tokens)
    {
        _tokens = tokens;
    }

    public bool AreInvalid()
    {
        return _tokens.Length != 2
               || string.IsNullOrWhiteSpace(_tokens[0])
               || string.IsNullOrWhiteSpace(_tokens[1]);
    }
    
    public bool CredentialsMatch(string user, string pass)
    {
        var usernameBytes = System.Text.Encoding.UTF8.GetBytes(Username ?? string.Empty);
        var userBytes     = System.Text.Encoding.UTF8.GetBytes(user);
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(Password ?? string.Empty);
        var passBytes     = System.Text.Encoding.UTF8.GetBytes(pass);

        return CryptographicOperations.FixedTimeEquals(usernameBytes, userBytes)
               && CryptographicOperations.FixedTimeEquals(passwordBytes, passBytes);
    }
}