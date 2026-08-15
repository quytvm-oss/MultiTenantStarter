using System.ComponentModel.DataAnnotations;

namespace Storage.S3;

public class S3StorageOptions
{
    [Required]
    public string? Bucket { get; set; }

    public string? Region { get; set; }

    public string? Prefix { get; set; }
    
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// True  → files are publicly readable; BuildPublicUrl returns a direct URL.
    /// False → bucket is private;           BuildPublicUrl returns a presigned URL.
    /// </summary>
    public bool PublicRead { get; set; } = true;
    
    /// <summary>TTL for presigned URLs when <see cref="PublicRead"/> is false.</summary>
    public TimeSpan PresignedUrlExpiry { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Multipart part size in bytes. AWS minimum is 5 MB; default here is 10 MB.
    /// Tune upward for very large files to reduce part count.
    /// </summary>
    public long MultipartPartSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Custom S3 endpoint URL. Set this to point at MinIO or any other S3-compatible
    /// service (e.g. "http://localhost:9000"). Leave empty to target AWS S3.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Explicit access key. When either <see cref="AccessKey"/> or <see cref="SecretKey"/>
    /// is empty, the AWS SDK's ambient credential chain is used instead.
    /// </summary>
    public string? AccessKey { get; set; }

    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Required for MinIO and most non-AWS S3-compatible services (they do not support
    /// virtual-hosted-style subdomains). Ignored when <see cref="ServiceUrl"/> is empty.
    /// </summary>
    public bool ForcePathStyle { get; set; }
}