using FluentValidation;

using Modules.Identity.Contracts.v1.Roles.DeleteRole;

namespace Modules.Identity.Features.v1.Roles.DeleteRole;

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}