using FluentValidation;

using Modules.Files.Contracts.v1.Commands;

namespace Modules.Files.Features.v1.RequestUploadUrl;

public class RequestUploadUrlCommandValidator : AbstractValidator<RequestUploadUrlCommand>
{
    public RequestUploadUrlCommandValidator()
    {
        RuleFor(x => x.OwnerType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.Visibility).IsInEnum();
    }
}
