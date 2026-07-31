using FluentValidation;

using Modules.Identity.Contracts.v1.Roles.UpdatePermissions;

namespace Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

public class UpdatePermissionsCommandValidator : AbstractValidator<UpdatePermissionsCommand>
{
    public UpdatePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty();
        RuleFor(x => x.Permissions)
            .NotNull();
    }
}