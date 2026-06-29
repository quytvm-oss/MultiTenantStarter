using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Modules.Identity.Domain;

namespace Modules.Identity.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Users", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
        
        builder.Property(x => x.ObjectId)
            .HasMaxLength(256);
    }
}

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Roles", IdentityModuleConstants.SchemaName)
            .IsMultiTenant()
            .AdjustUniqueIndexes();
    }
}

public class ApplicationRoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("RoleClaims", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
    }
}

public class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UserRoles", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
    }
}

public class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UserClaims", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();   
    }
}

public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UserLogins", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();  
    }
}

public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("UserTokens", IdentityModuleConstants.SchemaName)
            .IsMultiTenant(); 
    }
}