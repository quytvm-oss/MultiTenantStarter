using FluentValidation;

using Shared.Storage;

using Storage.Constant;

namespace Modules.Identity.Features.v1.Users;

public sealed class UserImageValidator : AbstractValidator<FileUploadRequest>
{
    public UserImageValidator() : this(FileType.Image) { }

    public UserImageValidator(FileType fileType)
    {
        var rules = FileTypeMetadata.GetRules(fileType);
        long maxSizeInBytes = rules.MaxSizeInMb * 1024L * 1024L;

        RuleFor(x => x.FileName).NotEmpty()
            .Must(fileName => rules.AllowedExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"Only these extensions are allowed: " + $"{string.Join(", ", rules.AllowedExtensions)}");

        RuleFor(x => x.ContentType).NotEmpty();

        RuleFor(x => x.Data)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("File stream is required.")
            .Must(stream => stream.Length > 0)
            .WithMessage("File must not be empty.")
            .Must(stream => stream.Length <= maxSizeInBytes)
            .WithMessage($"File must be <= {rules.MaxSizeInMb} MB.");
    }
}