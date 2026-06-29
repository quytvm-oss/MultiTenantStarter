using Microsoft.AspNetCore.Identity;

namespace Modules.Identity.Domain;

public sealed class RoleClaim : IdentityRoleClaim<string>
{
    public string? CreatedBy { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
}