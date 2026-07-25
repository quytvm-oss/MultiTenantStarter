using FluentValidation;

using Modules.Identity.Contracts.v1.Users.DeleteUser;

namespace Modules.Identity.Features.v1.Users.DeleteUser;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}