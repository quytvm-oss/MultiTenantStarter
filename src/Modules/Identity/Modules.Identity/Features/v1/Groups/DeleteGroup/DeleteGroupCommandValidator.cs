using FluentValidation;

using Modules.Identity.Contracts.v1.Groups.DeleteGroup;

namespace Modules.Identity.Features.v1.Groups.DeleteGroup;

public class DeleteGroupCommandValidator : AbstractValidator<DeleteGroupCommand>
{
    public DeleteGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Group ID is required.");
    }
}