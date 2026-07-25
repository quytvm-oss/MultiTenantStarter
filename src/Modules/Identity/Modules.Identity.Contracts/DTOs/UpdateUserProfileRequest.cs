using Microsoft.AspNetCore.Http;

namespace Modules.Identity.Contracts.DTOs;

public sealed class UpdateUserProfileRequest
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? PhoneNumber { get; init; }

    public bool DeleteCurrentImage { get; init; }

    public IFormFile? Image { get; init; }
}