using FluentValidation;

using Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;

namespace Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public class RemoveUserFromGroupCommandValidator : AbstractValidator<RemoveUserFromGroupCommand>
{
    public RemoveUserFromGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("GroupId is required");
        
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
    }
}