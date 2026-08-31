using FluentValidation;

using Modules.Files.Contracts.v1.Queries;

namespace Modules.Files.Features.v1.ListMyFiles;

public class ListMyFilesQueryValidator : AbstractValidator<ListMyFilesQuery>
{
    public ListMyFilesQueryValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
