using FluentValidation;

using Modules.Identity.Contracts.v1.Users.SearchUsers;

using Web.Validation;

namespace Modules.Identity.Features.v1.Users.SearchUsers;

public class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        Include(new PagedQueryValidator<SearchUsersQuery>());
        
        RuleFor(q => q.Search)
            .MaximumLength(200)
            .When(q => !string.IsNullOrEmpty(q.Search));

        RuleFor(q => q.RoleId)
            .MaximumLength(450)
            .When(q => !string.IsNullOrEmpty(q.RoleId));
    }
}