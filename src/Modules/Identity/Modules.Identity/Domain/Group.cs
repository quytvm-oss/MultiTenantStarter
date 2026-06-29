using Core.Domain;

namespace Modules.Identity.Domain;

public class Group : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsSystemGroup { get; private set; }
    
    // IAuditableEntity implementation
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // ISoftDeletable implementation
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    
    // Navigation properties
    public virtual ICollection<UserGroup> UserGroups { get; private set; } = [];
    
    public virtual ICollection<GroupRole> GroupRoles { get; private set; } = [];
    
    private Group() {}
}