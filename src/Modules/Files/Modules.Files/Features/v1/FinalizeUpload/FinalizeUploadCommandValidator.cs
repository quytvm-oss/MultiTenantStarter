using FluentValidation;

using Modules.Files.Contracts.v1.Commands;

namespace Modules.Files.Features.v1.FinalizeUpload;

public class FinalizeUploadCommandValidator : AbstractValidator<FinalizeUploadCommand>
{
    public FinalizeUploadCommandValidator()
    {
        RuleFor(x => x.FileAssetId).NotEmpty();
    }
}
