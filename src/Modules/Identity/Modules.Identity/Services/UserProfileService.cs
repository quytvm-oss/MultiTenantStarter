using Core.Exceptions;

using Finbuckle.MultiTenant.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Modules.Identity.Contracts.DTOs;
using Modules.Identity.Contracts.Services;
using Modules.Identity.Domain;

using Shared.Multitenancy;
using Shared.Storage;

using Storage.Abstractions;
using Storage.Constant;

using Web.Origin;

namespace Modules.Identity.Services;

internal sealed class UserProfileService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IStorageService storageService,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IOptions<OriginOptions> originOptions,
    IHttpContextAccessor httpContextAccessor) : IUserProfileService
{
    private readonly Uri? _originUrl = originOptions.Value.OriginUrl;
    
    private readonly string? _staticContentPath = originOptions.Value.StaticContentPath;
    
    public async Task<UserDto> GetAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        
        _ = user ?? throw new NotFoundException("user not found");

        return new UserDto()
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            ImageUrl = ResolveImageUrl(user.ImageUrl),
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    public async Task<List<UserDto>> GetListAsync(CancellationToken ct = default)
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync(ct);
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            result.Add(new UserDto()
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrl = ResolveImageUrl(user.ImageUrl),
                PhoneNumber = user.PhoneNumber,
            });
        }
        return result;
    }

    public Task<int> GetCountAsync(CancellationToken ct = default)
    => userManager.Users.AsNoTracking().CountAsync(ct);

    public async Task UpdateAsync(
        string userId,
        string firstName,
        string lastName,
        string phoneNumber,
        FileUploadRequest? image,
        bool deleteCurrentImage,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("user not found");

        var oldImagePath = user.ImageUrl?.ToString();
        string? newImagePath = null;

        if (image?.Data.Length > 0)
        {
            newImagePath = await storageService.UploadAsync<User>(image, FileType.Image, ct);
            user.ImageUrl = new Uri(newImagePath, UriKind.Relative);
        }
        else if (deleteCurrentImage)
        {
            user.ImageUrl = null;
        }

        user.FirstName = firstName;
        user.LastName = lastName;

        var currentPhoneNumber = await userManager.GetPhoneNumberAsync(user);
        if (phoneNumber != currentPhoneNumber)
        {
            await userManager.SetPhoneNumberAsync(user, phoneNumber);
        }

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(newImagePath))
            {
                await storageService.RemoveAsync(newImagePath, ct);
            }

            throw new CustomException("Update profile failed");
        }

        if (deleteCurrentImage && !string.IsNullOrWhiteSpace(oldImagePath))
        {
            await storageService.RemoveAsync(oldImagePath, ct);
        }

        await signInManager.RefreshSignInAsync(user);
    }

    public async Task SetImageUrlAsync(string userId, string? imageUrl, CancellationToken ct = default)
    {
        EnsureValidTenant();
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("user not found");
        
        user.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) 
            ? null : new Uri(imageUrl, UriKind.RelativeOrAbsolute);
        
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new CustomException("Update profile image failed");
        }
        
        await signInManager.RefreshSignInAsync(user);
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null, CancellationToken ct = default)
    {
        EnsureValidTenant();
        return await userManager.FindByEmailAsync(email.Normalize()) is { } user && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken ct = default)
    {
        EnsureValidTenant();
        return await userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null, CancellationToken ct = default)
    {
        EnsureValidTenant();
        return await userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, ct) is { } user && user.Id != exceptId;
    }

    #region internals
    
    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
    }

    private string? ResolveImageUrl(Uri? imageUrl)
    {
        if (imageUrl == null)
        {
            return null;
        }

        // Absolute URLs (e.g., S3) pass through unchanged.
        if (imageUrl.IsAbsoluteUri)
        {
            return imageUrl.ToString();
        }
        
        // For relative paths from local storage, prefix with the API origin and wwwroot.
        if (_originUrl is null)
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
            {
                var baseUri = $"{request.Scheme}://{request.Host.Value}{request.PathBase}";
                var relativePath = imageUrl.ToString().TrimStart('/');
                var prefixedRelativePath =  string.IsNullOrWhiteSpace(_staticContentPath)
                    ? relativePath
                    : $"{_staticContentPath.TrimStart('/')}/{relativePath}";
                return $"{baseUri.TrimEnd('/')}/{prefixedRelativePath}";
            }

            return imageUrl.ToString();
        }

        var originRelativePath = imageUrl.ToString().TrimStart('/');
        return $"{_originUrl.AbsoluteUri.TrimEnd('/')}/{originRelativePath}";
    }

    #endregion
}