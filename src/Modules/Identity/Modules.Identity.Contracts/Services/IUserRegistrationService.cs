using System.Security.Claims;

namespace Modules.Identity.Contracts.Services;

public interface IUserRegistrationService
{
    Task<string> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string userName,
        string password,
        string confirmPassword,
        string phoneNumber,
        string origin,
        CancellationToken ct = default);
    
    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    
    Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default);
    
    Task AdminConfirmEmailAsync(string userId, CancellationToken ct = default);
    
    Task ResendConfirmationEmailAsync(string userId, string origin, CancellationToken ct = default);
    
    Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default);
}