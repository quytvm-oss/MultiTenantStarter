using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;

using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

using Modules.Identity.Contracts.Services;
using Modules.Identity.Data;
using Modules.Identity.Domain;

using Shared.Identity;
using Shared.Multitenancy;

namespace Modules.Identity.Services;

public sealed class UserRegistrationService(
    UserManager<User> userManager,
    IdentityDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor) : IUserRegistrationService
{
    public async Task<string> RegisterAsync(string firstName, string lastName, string email, string userName, string password,
        string confirmPassword, string phoneNumber, string origin, CancellationToken ct = default)
    {
        ValidatePasswordMatch(password, confirmPassword);
        
        var user = await CreateUserWithPasswordAsync(firstName, lastName, email, userName, password, phoneNumber);
        await AssignDefaultRoleAndGroupsAsync(user, "System", ct);
        SendEmailConfirmation(user, origin);
        await PublishUserRegisteredAsync(user, "Identity", ct);
        return user.Id;
    }

    public async Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        EnsureValidTenant();
        ArgumentNullException.ThrowIfNull(principal);
        
        var email = ExtractEmailFromPrincipal(principal);
        
        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return existingUser.Id;
        }
        
        var user = await CreateUserFromPrincipalAsync(principal, email, ct);
        await AssignDefaultRoleAndGroupsAsync(user, "ExternalAuth", ct);
        await PublishUserRegisteredAsync(user,"Identity.ExternalAuth", ct);
        return user.Id;
    }

    public async Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken ct = default)
    {
        EnsureValidTenant();
        
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.EmailConfirmed, ct);
        
        _ = user ?? throw new CustomException("An error occurred while confirming E-Mail.");
        
        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, code);
        
        return result.Succeeded
            ? string.Format(CultureInfo.InvariantCulture, "Account Confirmed for E-Mail {0}. You can now use the /api/tokens endpoint to generate JWT.", user.Email)
            : throw new CustomException(string.Format(CultureInfo.InvariantCulture, "An error occurred while confirming {0}", user.Email));
    }

    public async Task AdminConfirmEmailAsync(string userId, CancellationToken ct = default)
    {
        EnsureValidTenant();
        
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException($"User {userId} was not found.");;
        
        _ = user ?? throw new CustomException("An error occurred while confirming E-Mail.");
        
        // Idempotent: a second confirm is a no-op rather than an error.
        if (user.EmailConfirmed)
        {
            return;
        }
        
        user.EmailConfirmed = true;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new CustomException(string.Format(
                CultureInfo.InvariantCulture,
                "An error occurred while confirming the email for {0}: {1}",
                user.Email,
                string.Join("; ", result.Errors.Select(e => e.Description))));
        }
    }

    public async Task ResendConfirmationEmailAsync(string userId, string origin, CancellationToken ct = default)
    {
        EnsureValidTenant();

        var user = await userManager.Users
                       .Where(u => u.Id == userId)
                       .FirstOrDefaultAsync(ct)
                   ?? throw new NotFoundException($"User {userId} was not found.");

        if (user.EmailConfirmed)
        {
            throw new CustomException(string.Format(
                CultureInfo.InvariantCulture,
                "The email for {0} is already confirmed.",
                user.Email));
        }
        
        SendEmailConfirmation(user, origin);
    }

    public async Task<string> ConfirmPhoneNumberAsync(string userId, string code, CancellationToken cancellationToken = default)
    {
        EnsureValidTenant();
        var user = await userManager.Users.FirstOrDefaultAsync(u=> u.Id == userId && 
                                                             !u.PhoneNumberConfirmed, cancellationToken);
        
        _ = user ?? throw new CustomException("An error occurred while confirming phone number.");
        
        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ChangePhoneNumberAsync(user, user.PhoneNumber!, code);
        
        return result.Succeeded
            ? string.Format(CultureInfo.InvariantCulture, "Phone number {0} confirmed successfully.", user.PhoneNumber)
            : throw new CustomException(string.Format(CultureInfo.InvariantCulture, "An error occurred while confirming phone number {0}", user.PhoneNumber));
        
    }

    #region internals

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
    }
    
    private static void ValidatePasswordMatch(string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            throw new CustomException(
                "Passwords do not match.",
                errors: null,
                HttpStatusCode.BadRequest);
        }
    }

    private static string ExtractEmailFromPrincipal(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? throw new CustomException("Email claim is required for external authentication.");
    }

    private static (string firstName, string lastName, string userName) ExtractUserInfoFromPrincipal(
        ClaimsPrincipal principal, string email)
    {
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName)
            ?? principal.FindFirstValue("given_name")
            ?? string.Empty;
        
        var lastName = principal.FindFirstValue(ClaimTypes.Surname)
            ?? principal.FindFirstValue("family_name")
            ?? string.Empty;

        var userName = principal.FindFirstValue(ClaimTypes.Name)
                       ?? principal.FindFirstValue("preferred_username")
                       ?? email.Split('@')[0];
        
        return (firstName, lastName, userName);
    }
    
    private async Task<string> EnsureUniqueUserNameAsync(string userName)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return $"{userName}_{Guid.CreateVersion7():N}"[..20];
        }

        return userName;
    }

    private async Task<User> CreateUserFromPrincipalAsync(ClaimsPrincipal principal, string email, CancellationToken ct)
    {
        var (firstName, lastName, userName) = ExtractUserInfoFromPrincipal(principal, email);

        userName = await EnsureUniqueUserNameAsync(userName);

        var user = new User()
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserName = userName,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            IsActive = true,
        };
        
        var result = await userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new CustomException(
                "Failed to create user from external principal.",
                errors,
                HttpStatusCode.BadRequest);
        }
        
        return user;
    }

    private async Task AssignDefaultRoleAndGroupsAsync(User user, string source, CancellationToken ct = default)
    {
        await userManager.AddToRoleAsync(user, RoleConstants.Basic);
        
        var defaultGroups = await db.Groups.AsNoTracking()
            .Where(g => g.IsDefault && !g.IsDeleted)
            .ToListAsync(ct);

        foreach (var group in defaultGroups)
        {
            await db.UserGroups.AddAsync(UserGroup.Create(user.Id, group.Id, source),ct);
        }

        if (defaultGroups.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<User> CreateUserWithPasswordAsync(
        string firstName,
        string lastName,
        string email,
        string userName,
        string password,
        string phoneNumber)
    {
        var user = new User()
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserName = userName,
            PhoneNumber = phoneNumber,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            IsActive = true,
        };
        
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            // Identity create failures (duplicate email/username, password policy, …) are
            // client-input errors, not server faults — surface them as 400 with the specific
            // reasons so the caller sees *why* registration failed, not a bare 500.
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new CustomException(
                "Unable to register the user.",
                errors,
                HttpStatusCode.BadRequest);
        }
        return user;
    }

    private async Task PublishUserRegisteredAsync(User user,string source, CancellationToken cancellationToken = default)
    {
        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        user.RecordRegistered(tenantId,source);

        await db.SaveChangesAsync(cancellationToken);
    }

    private void SendEmailConfirmation(User user, string origin)
    {
        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        user.RequestEmailConfirmation(tenantId!, origin);
    }

    #endregion
}