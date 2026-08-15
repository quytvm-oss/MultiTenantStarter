namespace Modules.Files.Contracts;


/// <summary>
/// Per-OwnerType authorization for FileAssets. Each owning module (Catalog, Tickets, ...) registers
/// its own implementation via <c>services.AddFileAccessPolicy&lt;TPolicy&gt;()</c>. The Files module
/// ships a uploader-only default for the built-in <c>MyFiles</c> and <c>User</c> owner types.
/// Tenant scoping is enforced by the framework's BaseDbContext (schema-per-tenant) and is NOT
/// delegated to policies. Policies receive a primitive <c>currentUserId</c> rather than a
/// <c>ClaimsPrincipal</c> so the contract stays free of ASP.NET Core types — owning modules that
/// need richer authz can inject their own dependencies.
/// </summary>
public interface IFileAccessPolicy
{
    /// <summary>The OwnerType this policy handles. Must be unique across registered policies.</summary>
    string OwnerType { get; }

    Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken);
    
    Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken);
    
    Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the caller may change a file's <see cref="FileAccessContext.Visibility"/> after
    /// upload. Defaults to the same rule as <see cref="CanDeleteAsync"/> — only the uploader.
    /// Modules whose files are tied to a domain entity (Catalog product images, Chat attachments)
    /// can override to disallow visibility flips entirely.
    /// </summary>
    Task<bool> CanChangeVisibilityAsync(FileAccessContext context, string currentUserId,
        CancellationToken cancellationToken) => CanDeleteAsync(context, currentUserId, cancellationToken);
}