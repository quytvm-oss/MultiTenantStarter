using System.Security.Claims;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Jobs.Services;

using Mailling.Abstractions;

using MessageBus;

using Microsoft.AspNetCore.Identity;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

using Shared.Multitenancy;

namespace Modules.Identity.Services;

public sealed class UserRegistrationService(
    UserManager<User> userManager,
    IdentityDbContext db,
    IJobService jobService,
    IMailService mailService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IBusPublisher bus) : IUserRegistrationService
{
    public Task<string> RegisterAsync(string firstName, string lastName, string email, string userName, string password,
        string confirmPassword, string phoneNumber, string origin, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        EnsureValidTenant();
        ArgumentNullException.ThrowIfNull(principal);
        
        
        
        throw new NotImplementedException();
    }

    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AdminConfirmEmailAsync(string userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task ResendConfirmationEmailAsync(string userId, string origin, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #region internals

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
    }

    #endregion
}