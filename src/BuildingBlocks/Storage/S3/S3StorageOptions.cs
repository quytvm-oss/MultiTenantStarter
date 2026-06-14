using System.ComponentModel.DataAnnotations;

namespace Storage.S3;

public class S3StorageOptions
{
    [Required]
    public string? Bucket { get; set; }

    public string? Region { get; set; }
    
    public string? AccessKey { get; set; }

    public string? SecretAccessKey { get; set; }

    public string? Prefix { get; set; }
    
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// True  → files are publicly readable; BuildPublicUrl returns a direct URL.
    /// False → bucket is private;           BuildPublicUrl returns a presigned URL.
    /// </summary>
    public bool PublicRead { get; set; }
    
    /// <summary>TTL for presigned URLs when <see cref="PublicRead"/> is false.</summary>
    public TimeSpan PresignedUrlExpiry { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Multipart part size in bytes. AWS minimum is 5 MB; default here is 10 MB.
    /// Tune upward for very large files to reduce part count.
    /// </summary>
    public long MultipartPartSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
}