using System.Diagnostics.CodeAnalysis;

using Finbuckle.MultiTenant.Abstractions;

namespace Shared.Multitenancy;

public class AppTenantInfo : TenantInfo, IAppTenantInfo
{
    [SetsRequiredMembers]
    public AppTenantInfo()
    {
        Id = string.Empty;
        Identifier = string.Empty;
    }
    
    [SetsRequiredMembers]
    public AppTenantInfo(string id, string identifier, string? name = null)
    {
        Id = id;
        Identifier = identifier;
        Name = name;
    }

    [SetsRequiredMembers]
    public AppTenantInfo(string id, string name, string? connectionString, string adminEmail, string? issuer = null)
    : this(id, id, name)
    {
        ConnectionString = connectionString ?? string.Empty;
        AdminEmail = adminEmail;
        Issuer = issuer;
        IsActive = true;

        ValidUpTo = TimeProvider.System.GetUtcNow().UtcDateTime.AddMonths(1);
    }
    
    public string? ConnectionString { get; set; } = string.Empty;
    
    public string AdminEmail { get; set; }
    
    public string? Issuer { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime ValidUpTo { get; set;}

    public void AddValidity(int months)
    {
        ValidUpTo = ValidUpTo.AddMonths(months);
    }

    public void SeValidity(in DateTime validTill)
    {
        var normalized = validTill;
        ValidUpTo = ValidUpTo < normalized ? normalized : throw new InvalidOperationException("Subscription cannot be backdated.");
    }

    public void Activate()
    {
        if (Id == MultitenancyConstants.Root.Id)
        {
            throw new InvalidOperationException("Invalid tenant");
        }
        
        IsActive = true;
    }
    
 
    public void Deactivate()
    {
        if (Id == MultitenancyConstants.Root.Id)
        {
            throw new InvalidOperationException("Invalid tenant");
        }
        
        IsActive = false;
    }

    string? IAppTenantInfo.ConnectionString
    {
        get => ConnectionString;
        set => ConnectionString = value ?? throw new InvalidOperationException("Connection string cannot be null");
    }
}