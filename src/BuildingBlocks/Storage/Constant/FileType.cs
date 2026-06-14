namespace Storage.Constant;

public enum FileType
{
    Image, 
    Document,
    Video,
    Backup
}

internal sealed record FileTypeRules(
    IReadOnlyList<string> AllowedExtensions,
    int MaxSizeInMb);

internal static class FileTypeMetadata
{
    private static readonly Dictionary<FileType, FileTypeRules> _rules = new()
    {
        [FileType.Image] = new FileTypeRules(
            AllowedExtensions: [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"],
            MaxSizeInMb: 10),

        [FileType.Document] = new FileTypeRules(
            AllowedExtensions: [".pdf", ".docx", ".xlsx", ".pptx", ".txt"],
            MaxSizeInMb: 50),

        [FileType.Video] = new FileTypeRules(
            AllowedExtensions: [".mp4", ".mov", ".avi", ".mkv", ".webm"],
            MaxSizeInMb: 500),

        [FileType.Backup] = new FileTypeRules(
            AllowedExtensions: [".zip", ".tar", ".gz", ".7z"],
            MaxSizeInMb: 2048)
    };
    
    public static FileTypeRules GetRules(FileType fileType)
        => _rules.TryGetValue(fileType, out var rules)
            ? rules
            : throw new ArgumentOutOfRangeException(nameof(fileType), $"No rules defined for {fileType}.");
}