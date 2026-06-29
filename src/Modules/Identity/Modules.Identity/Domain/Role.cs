using Microsoft.AspNetCore.Identity;

namespace Modules.Identity.Domain;

public sealed class Role : IdentityRole
{
    public string? Description { get; set; }
    
    public Role(string name, string? description = null)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Description = description;
        NormalizedName = name.ToUpperInvariant();
    }
}