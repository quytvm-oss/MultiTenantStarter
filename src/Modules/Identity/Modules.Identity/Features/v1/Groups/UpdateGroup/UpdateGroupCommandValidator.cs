using FluentValidation;

using Modules.Identity.Contracts.v1.Groups.UpdateGroup;

namespace Modules.Identity.Features.v1.Groups.UpdateGroup;

public class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required")
            .MaximumLength(256).WithMessage("Group name must not exceed 256 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(1024).WithMessage("Group description must not exceed 1024 characters");
        
    }
}