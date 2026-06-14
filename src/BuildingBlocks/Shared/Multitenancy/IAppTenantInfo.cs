namespace Shared.Multitenancy;

public interface IAppTenantInfo
{
    string? ConnectionString { get; set; }
}