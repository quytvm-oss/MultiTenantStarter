using FluentValidation;

using Modules.Identity.Contracts.v1.Users.AssignUserRoles;

namespace Modules.Identity.Features.v1.Users.AssignUserRoles;

public class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
        
        RuleFor(x => x.UserRoles)
            .NotNull().WithMessage("User roles are required.");       
    }
}