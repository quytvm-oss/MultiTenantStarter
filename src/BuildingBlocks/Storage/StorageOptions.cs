using System.ComponentModel.DataAnnotations;

using Storage.Local;
using Storage.S3;

namespace Storage;

public class StorageOptions
{
    
    [Required]
    public string? Provider { get; set; } = "Local";
    
    public LocalStorageOptions? Local { get; set; }
    
    public S3StorageOptions? S3 { get; set; }
}