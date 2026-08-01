using FluentValidation;

using Modules.Identity.Contracts.v1.Groups.AddUsersToGroup;

namespace Modules.Identity.Features.v1.Groups.AddUsersToGroup;

public class AddUsersToGroupCommandValidator : AbstractValidator<AddUsersToGroupCommand>
{
    public AddUsersToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("GroupId is required");
        
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user ID is required.")
            .Must(ids => ids.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("User IDs cannot be empty or whitespace.");
    }
}