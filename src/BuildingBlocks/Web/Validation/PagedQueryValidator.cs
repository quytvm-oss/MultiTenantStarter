using FluentValidation;

using Shared.Persistence;

namespace Web.Validation;

public class PagedQueryValidator<T> : AbstractValidator<T>
   where T : IPagedQuery
{
    public PagedQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThan(0)
            .When(q => q.PageNumber.HasValue)
            .WithMessage("Page Number must be greater than zero");
        
        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .When(q => q.PageSize.HasValue)
            .WithMessage("Page Size must be between 1 and 100");
        
        RuleFor(q => q.Sort)
            .MaximumLength(200)
            .When(q => !string.IsNullOrEmpty(q.Sort))
            .WithMessage("Sort expression must not exceed 200 characters.");
    }
}