using System.ComponentModel.DataAnnotations;

namespace Storage.Local;

public class LocalStorageOptions
{
    [Required]
    public string? StorageRoot { get; set; } = string.Empty;
}