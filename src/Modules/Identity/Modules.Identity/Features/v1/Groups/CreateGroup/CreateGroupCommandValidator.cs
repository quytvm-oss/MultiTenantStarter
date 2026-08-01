using FluentValidation;

using Modules.Identity.Contracts.v1.Groups.CreateGroup;

namespace Modules.Identity.Features.v1.Groups.CreateGroup;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MinimumLength(256).WithMessage("Group name must not exceed 256 characters.");
        
        RuleFor(x => x.Description)
            .MinimumLength(1024).WithMessage("Group description must not exceed 1024 characters.");
    }
}