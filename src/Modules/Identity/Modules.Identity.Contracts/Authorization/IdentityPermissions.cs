namespace Modules.Identity.Contracts.Authorization;

public static class IdentityPermissions
{
    public static class Users
    {
        public const string Resource = nameof(Users);
        public const string View          = $"Permissions.{Resource}.View";
        public const string Create        = $"Permissions.{Resource}.Create";
        public const string Update        = $"Permissions.{Resource}.Update";
        public const string Delete        = $"Permissions.{Resource}.Delete";
        public const string Export        = $"Permissions.{Resource}.Export";
        public const string ManageRoles   = $"Permissions.{Resource}.ManageRoles";
        public const string Impersonate   = $"Permissions.{Resource}.Impersonate";
        public const string ConfirmEmail  = $"Permissions.{Resource}.ConfirmEmail";
    }

    public static class UserRoles
    {
        public const string Resource = nameof(UserRoles);
        public const string View   = $"Permissions.{Resource}.View";
        public const string Update = $"Permissions.{Resource}.Update";
    }

    public static class Roles
    {
        public const string Resource = nameof(Roles);
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class RoleClaims
    {
        public const string Resource = nameof(RoleClaims);
        public const string View   = $"Permissions.{Resource}.View";
        public const string Update = $"Permissions.{Resource}.Update";
    }

    public static class Sessions
    {
        public const string Resource = nameof(Sessions);
        public const string View      = $"Permissions.{Resource}.View";
        public const string Revoke    = $"Permissions.{Resource}.Revoke";
        public const string ViewAll   = $"Permissions.{Resource}.ViewAll";
        public const string RevokeAll = $"Permissions.{Resource}.RevokeAll";
    }
    
    public static class Groups
    {
        public const string Resource = nameof(Groups);
        public const string View          = $"Permissions.{Resource}.View";
        public const string Create        = $"Permissions.{Resource}.Create";
        public const string Update        = $"Permissions.{Resource}.Update";
        public const string Delete        = $"Permissions.{Resource}.Delete";
        public const string ManageMembers = $"Permissions.{Resource}.ManageMembers";
    }
}